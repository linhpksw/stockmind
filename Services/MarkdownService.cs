using System.Collections.Generic;
using System.Linq;
using stockmind.Commons.Attributes;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.DTOs.Markdowns;
using stockmind.Models;
using stockmind.Repositories;
using stockmind.Utils;

namespace stockmind.Services
{
    public class MarkdownService
    {
        private readonly LotRepository _lotRepository;
        private readonly MarkdownRuleRepository _ruleRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly ProductRepository _productRepository;
        private readonly SalesOrderItemRepository _salesOrderItemRepository;
        private readonly LotSaleDecisionRepository _lotSaleDecisionRepository;
        private readonly ILogger<MarkdownService> _logger;

        public MarkdownService(
            LotRepository lotRepository,
            MarkdownRuleRepository ruleRepository,
            CategoryRepository categoryRepository,
            ProductRepository productRepository,
            SalesOrderItemRepository salesOrderItemRepository,
            LotSaleDecisionRepository lotSaleDecisionRepository,
            ILogger<MarkdownService> logger)
        {
            _lotRepository = lotRepository;
            _ruleRepository = ruleRepository;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _salesOrderItemRepository = salesOrderItemRepository;
            _lotSaleDecisionRepository = lotSaleDecisionRepository;
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
            var lotIds = lots.Select(lot => lot.LotId).ToList();
            var latestDecisions = await _lotSaleDecisionRepository.GetLatestByLotIdsAsync(lotIds, cancellationToken);
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

                if (!TryGetLatestUnitCost(lot, out var unitCost, out var qtyReceived))
                {
                    _logger.LogWarning("Skipping lot {LotId} because no GRN cost history was found.", lot.LotId);
                    continue;
                }

                var targetDiscount = Clamp(rule.DiscountPercent, 0m, 1m);
                var floorSafeDiscount = ApplyFloorGuard(lot.Product.Price, unitCost, rule.FloorPercentOfCost, targetDiscount);
                var requiresFloorOverride = floorSafeDiscount > 0 && floorSafeDiscount + 0.00005m < targetDiscount;

                if (floorSafeDiscount <= 0 && targetDiscount <= 0)
                {
                    _logger.LogDebug(
                        "Lot {LotId} for product {ProductId} cannot be discounted without breaking floor guard.",
                        lot.LotId,
                        lot.Product.ProductId);
                    continue;
                }

                latestDecisions.TryGetValue(lot.LotId, out var decision);

                recommendations.Add(new MarkdownRecommendationDto
                {
                    ProductId = lot.Product.SkuCode,
                    ProductName = lot.Product.Name,
                    LotId = lot.LotCode,
                    LotEntityId = lot.LotId,
                    CategoryId = lot.Product.CategoryId,
                    CategoryName = lot.Product.Category?.Name,
                    UnitCost = unitCost,
                    ListPrice = lot.Product.Price,
                    QtyReceived = qtyReceived,
                    LotSaleDecisionId = decision?.LotSaleDecisionId,
                    LotSaleDecisionApplied = decision?.IsApplied ?? false,
                    ReceivedAt = lot.ReceivedAt,
                    DaysToExpiry = daysToExpiry,
                    SuggestedDiscountPct = decimal.Round(targetDiscount, 4, MidpointRounding.AwayFromZero),
                    FloorPctOfCost = rule.FloorPercentOfCost,
                    FloorSafeDiscountPct = requiresFloorOverride
                        ? decimal.Round(floorSafeDiscount, 4, MidpointRounding.AwayFromZero)
                        : null,
                    RequiresFloorOverride = requiresFloorOverride
                });
            }

            return recommendations
                .OrderBy(r => r.DaysToExpiry)
                .ThenBy(r => r.ProductId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<List<MarkdownRuleDto>> ListRulesAsync(CancellationToken cancellationToken)
        {
            var rules = await _ruleRepository.ListAsync(cancellationToken);
            if (rules.Count == 0)
            {
                return new List<MarkdownRuleDto>();
            }

            return rules
                .OrderBy(rule => rule.CategoryId.HasValue ? 1 : 0)
                .ThenBy(rule => rule.Category?.Name ?? "GLOBAL", StringComparer.OrdinalIgnoreCase)
                .ThenBy(rule => rule.DaysToExpiry)
                .Select(MarkdownRuleMapper.ToDto)
                .ToList();
        }

        [Transactional]
        public async Task<MarkdownRuleDto> CreateRuleAsync(
            MarkdownRuleRequestDto request,
            CancellationToken cancellationToken)
        {
            var normalized = ValidateRuleRequest(request);
            var scope = await ResolveRuleScopeAsync(request?.CategoryId, cancellationToken);

            await EnsureRuleIsUniqueAsync(scope.CategoryId, normalized.DaysToExpiry, null, cancellationToken);

            var utcNow = DateTime.UtcNow;
            var rule = new MarkdownRule
            {
                CategoryId = scope.CategoryId,
                DaysToExpiry = normalized.DaysToExpiry,
                DiscountPercent = normalized.DiscountPercent,
                FloorPercentOfCost = normalized.FloorPercentOfCost,
                CreatedAt = utcNow,
                LastModifiedAt = utcNow,
                Deleted = false
            };

            await _ruleRepository.AddAsync(rule, cancellationToken);

            _logger.LogInformation(
                "Created markdown rule {RuleId} for {Scope} at D-{Days}.",
                rule.MarkdownRuleId,
                DescribeScope(scope),
                normalized.DaysToExpiry);

            var persisted = await _ruleRepository.GetDetailedByIdAsync(rule.MarkdownRuleId, cancellationToken) ?? rule;
            return MarkdownRuleMapper.ToDto(persisted);
        }

        [Transactional]
        public async Task<MarkdownRuleDto> UpdateRuleAsync(
            long ruleId,
            MarkdownRuleRequestDto request,
            CancellationToken cancellationToken)
        {
            var normalized = ValidateRuleRequest(request);
            var scope = await ResolveRuleScopeAsync(request?.CategoryId, cancellationToken);

            var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken)
                       ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { ruleId.ToString() });

            await EnsureRuleIsUniqueAsync(scope.CategoryId, normalized.DaysToExpiry, rule.MarkdownRuleId, cancellationToken);

            rule.CategoryId = scope.CategoryId;
            rule.DaysToExpiry = normalized.DaysToExpiry;
            rule.DiscountPercent = normalized.DiscountPercent;
            rule.FloorPercentOfCost = normalized.FloorPercentOfCost;
            rule.LastModifiedAt = DateTime.UtcNow;

            await _ruleRepository.UpdateAsync(rule, cancellationToken);

            _logger.LogInformation(
                "Updated markdown rule {RuleId} for {Scope} at D-{Days}.",
                rule.MarkdownRuleId,
                DescribeScope(scope),
                normalized.DaysToExpiry);

            var persisted = await _ruleRepository.GetDetailedByIdAsync(rule.MarkdownRuleId, cancellationToken) ?? rule;
            return MarkdownRuleMapper.ToDto(persisted);
        }

        [Transactional]
        public async Task<MarkdownRuleDto> DeleteRuleAsync(
            long ruleId,
            CancellationToken cancellationToken)
        {
            var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken)
                       ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { ruleId.ToString() });

            var detailed = await _ruleRepository.GetDetailedByIdAsync(ruleId, cancellationToken) ?? rule;

            rule.Deleted = true;
            rule.LastModifiedAt = DateTime.UtcNow;

            await _ruleRepository.SoftDeleteAsync(rule, cancellationToken);

            _logger.LogInformation(
                "Deleted markdown rule {RuleId} for {Scope} at D-{Days}.",
                rule.MarkdownRuleId,
                DescribeScope(new RuleScope(rule.CategoryId, detailed.Category)),
                rule.DaysToExpiry);

            detailed.Deleted = true;
            detailed.LastModifiedAt = rule.LastModifiedAt;
            return MarkdownRuleMapper.ToDto(detailed);
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

            if (!TryGetLatestUnitCost(lot, out var unitCost, out _))
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

            await _lotSaleDecisionRepository.AddAsync(lot.LotId, discountPct, true, cancellationToken);

            return new MarkdownApplyResponseDto
            {
                Applied = true,
                EffectivePrice = effectivePrice
            };
        }

        public async Task<MarkdownApplyBulkResponseDto> ApplyMarkdownsAsync(
            MarkdownApplyBulkRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request?.Items == null || request.Items.Count == 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "items" });
            }

            var response = new MarkdownApplyBulkResponseDto
            {
                Requested = request.Items.Count
            };

            foreach (var item in request.Items)
            {
                try
                {
                    await ApplyMarkdownAsync(item, cancellationToken);
                    response.Applied++;
                }
                catch (BizException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to apply markdown for product {ProductId} lot {LotId}.",
                        item?.ProductId,
                        item?.LotId);
                    response.Errors.Add(
                        $"{item?.ProductId ?? "?"}/{item?.LotId ?? "?"}: {ex.Message}{FormatParams(ex.Params)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected error applying markdown for product {ProductId} lot {LotId}.",
                        item?.ProductId,
                        item?.LotId);
                    response.Errors.Add($"{item?.ProductId ?? "?"}/{item?.LotId ?? "?"}: {ex.Message}");
                }
            }

            response.Failed = response.Requested - response.Applied;
            return response;
        }

        public async Task RevertLotSaleDecisionAsync(long decisionId, CancellationToken cancellationToken)
        {
            var updated = await _lotSaleDecisionRepository.UpdateIsAppliedAsync(decisionId, false, cancellationToken);
            if (!updated)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"lotSaleDecisionId={decisionId}" });
            }
        }

        private static string FormatParams(IReadOnlyCollection<string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return string.Empty;
            }

            return $" ({string.Join(", ", parameters)})";
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

        private static bool TryGetLatestUnitCost(Lot lot, out decimal unitCost, out decimal qtyReceived)
        {
            unitCost = 0m;
            qtyReceived = 0m;
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
            qtyReceived = grnItem.QtyReceived;
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

        private static RuleRequestValues ValidateRuleRequest(MarkdownRuleRequestDto? request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.DaysToExpiry < 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "daysToExpiry" });
            }

            if (request.DiscountPercent <= 0m || request.DiscountPercent > 1m)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "discountPercent" });
            }

            if (request.FloorPercentOfCost < 0m || request.FloorPercentOfCost > 1m)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "floorPercentOfCost" });
            }

            return new RuleRequestValues(request.DaysToExpiry, request.DiscountPercent, request.FloorPercentOfCost);
        }

        private async Task<RuleScope> ResolveRuleScopeAsync(string? categoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                return new RuleScope(null, null);
            }

            var trimmed = categoryId.Trim();
            if (!long.TryParse(trimmed, out var parsed) || parsed <= 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "categoryId" });
            }

            var category = await _categoryRepository.GetByIdAsync(parsed, cancellationToken)
                           ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { trimmed });

            return new RuleScope(parsed, category);
        }

        private async Task EnsureRuleIsUniqueAsync(long? categoryId, int daysToExpiry, long? excludeRuleId, CancellationToken cancellationToken)
        {
            var exists = await _ruleRepository.ExistsForScopeAsync(categoryId, daysToExpiry, excludeRuleId, cancellationToken);
            if (exists)
            {
                var scopeLabel = categoryId.HasValue ? $"category {categoryId.Value}" : "global scope";
                throw new BizDataAlreadyExistsException(
                    ErrorCode4xx.DataAlreadyExists,
                    new[] { $"{scopeLabel} D-{daysToExpiry}" });
            }
        }

        private static string DescribeScope(RuleScope scope)
        {
            if (scope.CategoryId.HasValue)
            {
                var label = string.IsNullOrWhiteSpace(scope.Category?.Name)
                    ? $"Category #{scope.CategoryId.Value}"
                    : $"{scope.Category!.Name} (#{scope.CategoryId.Value})";
                return label;
            }

            return "GLOBAL";
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

        private sealed record RuleRequestValues(int DaysToExpiry, decimal DiscountPercent, decimal FloorPercentOfCost);

        private sealed record RuleScope(long? CategoryId, Category? Category);

        private sealed record RuleLookup(
            IReadOnlyDictionary<long, List<MarkdownRule>> CategoryRules,
            IReadOnlyList<MarkdownRule> GlobalRules);
    }
}
