using stockmind.Commons.Attributes;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.DTOs.Markdowns;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class MarkdownService
    {
        private readonly LotRepository _lotRepository;
        private readonly MarkdownRuleRepository _ruleRepository;
        private readonly ProductRepository _productRepository;
        private readonly SalesOrderItemRepository _salesOrderItemRepository;
        private readonly ILogger<MarkdownService> _logger;

        public MarkdownService(
            LotRepository lotRepository,
            MarkdownRuleRepository ruleRepository,
            ProductRepository productRepository,
            SalesOrderItemRepository salesOrderItemRepository,
            ILogger<MarkdownService> logger)
        {
            _lotRepository = lotRepository;
            _ruleRepository = ruleRepository;
            _productRepository = productRepository;
            _salesOrderItemRepository = salesOrderItemRepository;
            _logger = logger;
        }

        public async Task<List<MarkdownRecommendationDto>> GetRecommendationsAsync(
            int daysThreshold,
            CancellationToken cancellationToken)
        {
            if (daysThreshold <= 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "days" });
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            _logger.LogInformation("Generating markdown recommendations for the next {Days} days (UTC date {Today})", daysThreshold, today);

            var lots = await _lotRepository.GetPerishableLotsExpiringWithinAsync(daysThreshold, cancellationToken);
            var rules = await _ruleRepository.GetActiveRulesAsync(cancellationToken);

            if (rules.Count == 0)
            {
                _logger.LogWarning("No markdown rules configured; unable to produce recommendations.");
                return new List<MarkdownRecommendationDto>();
            }

            var ruleLookup = BuildRuleLookup(rules);
            var recommendations = new List<MarkdownRecommendationDto>();

            foreach (var lot in lots)
            {
                var daysToExpiry = CalculateDaysToExpiry(lot, today);
                if (daysToExpiry < 0)
                {
                    continue;
                }

                var rule = ResolveRule(lot.Product.CategoryId, daysToExpiry, ruleLookup);
                if (rule == null)
                {
                    var categoryLabel = lot.Product.CategoryId?.ToString() ?? "UNCATEGORIZED";
                    _logger.LogWarning(
                        "Skipping lot {LotId} because no markdown rule configured for category {CategoryId} at D-{Days}.",
                        lot.LotId,
                        categoryLabel,
                        daysToExpiry);
                    continue;
                }

                if (!TryGetLatestUnitCost(lot, out var unitCost))
                {
                    _logger.LogWarning("Skipping lot {LotId} because no GRN cost history was found.", lot.LotId);
                    continue;
                }

                var adjustedDiscount = ApplyFloorGuard(lot.Product.Price, unitCost, rule.FloorPercentOfCost, rule.DiscountPercent);
                if (adjustedDiscount <= 0)
                {
                    _logger.LogDebug(
                        "Lot {LotId} for product {ProductId} cannot be discounted without breaking floor guard.",
                        lot.LotId,
                        lot.Product.ProductId);
                    continue;
                }

                recommendations.Add(new MarkdownRecommendationDto
                {
                    ProductId = lot.Product.SkuCode,
                    LotId = lot.LotCode,
                    DaysToExpiry = daysToExpiry,
                    SuggestedDiscountPct = decimal.Round(adjustedDiscount, 4, MidpointRounding.AwayFromZero),
                    FloorPctOfCost = rule.FloorPercentOfCost
                });
            }

            return recommendations
                .OrderBy(r => r.DaysToExpiry)
                .ThenBy(r => r.ProductId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        [Transactional]
        public async Task<MarkdownApplyResponseDto> ApplyMarkdownAsync(
            MarkdownApplyRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var productCode = NormalizeRequired(request.ProductId, "productId");
            var lotCode = NormalizeRequired(request.LotId, "lotId");
            var discountPct = Clamp(request.DiscountPct, 0m, 1m);

            if (discountPct <= 0m)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "discountPct" });
            }

            var product = await _productRepository.FindBySkuAsync(productCode, cancellationToken)
                          ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { productCode });

            var lot = await _lotRepository.GetLotWithHistoryAsync(product.ProductId, lotCode, cancellationToken)
                      ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { lotCode });

            if (!lot.ExpiryDate.HasValue)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "lot expiry required for perishable markdown" });
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var daysToExpiry = CalculateDaysToExpiry(lot, today);
            if (daysToExpiry < 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "lot already expired" });
            }

            var rules = await _ruleRepository.GetActiveRulesAsync(cancellationToken);
            if (rules.Count == 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "markdown rules missing" });
            }

            var lookup = BuildRuleLookup(rules);
            var rule = ResolveRule(product.CategoryId, daysToExpiry, lookup)
                       ?? throw new BizException(ErrorCode4xx.InvalidInput, new[] { $"no markdown rule for D-{daysToExpiry}" });

            if (!TryGetLatestUnitCost(lot, out var unitCost))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "unit cost history missing" });
            }

            var floorPrice = unitCost * rule.FloorPercentOfCost;
            var rawPrice = product.Price * (1 - discountPct);
            var effectivePrice = decimal.Round(rawPrice, 2, MidpointRounding.AwayFromZero);

            if (!request.OverrideFloor && rawPrice < floorPrice)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "discount breaks floor constraint" });
            }

            var affected = await _salesOrderItemRepository.ApplyMarkdownAsync(product.ProductId, discountPct, cancellationToken);
            _logger.LogInformation(
                "Applied markdown {Discount:P2} to product {ProductCode} (lot {LotCode}). Floor override={Override}. Affected order items={Affected}.",
                discountPct,
                product.SkuCode,
                lotCode,
                request.OverrideFloor,
                affected);

            return new MarkdownApplyResponseDto
            {
                Applied = true,
                EffectivePrice = effectivePrice
            };
        }

        private static RuleLookup BuildRuleLookup(IEnumerable<MarkdownRule> rules)
        {
            var categoryRules = new Dictionary<long, List<MarkdownRule>>();
            var globalRules = new List<MarkdownRule>();

            foreach (var rule in rules)
            {
                var targetList = rule.CategoryId.HasValue
                    ? GetOrAdd(categoryRules, rule.CategoryId.Value)
                    : globalRules;

                targetList.Add(rule);
            }

            foreach (var list in categoryRules.Values)
            {
                list.Sort(static (a, b) => a.DaysToExpiry.CompareTo(b.DaysToExpiry));
            }

            globalRules.Sort(static (a, b) => a.DaysToExpiry.CompareTo(b.DaysToExpiry));
            return new RuleLookup(categoryRules, globalRules);
        }

        private static int CalculateDaysToExpiry(Lot lot, DateOnly today)
        {
            return lot.ExpiryDate.HasValue
                ? lot.ExpiryDate.Value.DayNumber - today.DayNumber
                : int.MaxValue;
        }

        private static MarkdownRule? ResolveRule(
            long? categoryId,
            int daysToExpiry,
            RuleLookup ruleLookup)
        {
            if (categoryId.HasValue &&
                TryGetRule(ruleLookup.CategoryRules, categoryId.Value, daysToExpiry, out var scopedRule))
            {
                return scopedRule;
            }

            if (TryGetRule(ruleLookup.GlobalRules, daysToExpiry, out var globalRule))
            {
                return globalRule;
            }

            return null;
        }

        private static bool TryGetRule(
            IReadOnlyDictionary<long, List<MarkdownRule>> ruleLookup,
            long categoryId,
            int daysToExpiry,
            out MarkdownRule? rule)
        {
            rule = null;
            if (!ruleLookup.TryGetValue(categoryId, out var rules))
            {
                return false;
            }

            rule = rules.FirstOrDefault(r => r.DaysToExpiry == daysToExpiry);
            return rule != null;
        }

        private static bool TryGetRule(
            IReadOnlyList<MarkdownRule> rules,
            int daysToExpiry,
            out MarkdownRule? rule)
        {
            rule = rules.FirstOrDefault(r => r.DaysToExpiry == daysToExpiry);
            return rule != null;
        }

        private static List<MarkdownRule> GetOrAdd(Dictionary<long, List<MarkdownRule>> dictionary, long categoryId)
        {
            if (!dictionary.TryGetValue(categoryId, out var list))
            {
                list = new List<MarkdownRule>();
                dictionary[categoryId] = list;
            }

            return list;
        }

        private static bool TryGetLatestUnitCost(Lot lot, out decimal unitCost)
        {
            unitCost = 0m;
            if (lot.Grnitems is null || lot.Grnitems.Count == 0)
            {
                return false;
            }

            var grnItem = lot.Grnitems
                .Where(item => !item.Deleted)
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefault();

            if (grnItem == null)
            {
                return false;
            }

            unitCost = grnItem.UnitCost;
            return unitCost >= 0;
        }

        private static decimal ApplyFloorGuard(decimal listPrice, decimal unitCost, decimal floorPercent, decimal proposedDiscount)
        {
            if (listPrice <= 0)
            {
                return 0;
            }

            var normalizedDiscount = Clamp(proposedDiscount, 0m, 1m);
            var floorPrice = unitCost * floorPercent;
            if (floorPrice <= 0)
            {
                return normalizedDiscount;
            }

            var maxDiscount = 1m - (floorPrice / listPrice);
            if (maxDiscount <= 0)
            {
                return 0;
            }

            return Math.Min(normalizedDiscount, maxDiscount);
        }

        private static decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static string NormalizeRequired(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { fieldName });
            }

            return value.Trim();
        }

        private sealed record RuleLookup(
            IReadOnlyDictionary<long, List<MarkdownRule>> CategoryRules,
            IReadOnlyList<MarkdownRule> GlobalRules);
    }
}
