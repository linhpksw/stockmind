using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using stockmind.Commons.Attributes;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Responses;
using stockmind.DTOs.SalesOrders;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class SalesOrderService
    {
        private static readonly JsonSerializerOptions PendingSerializerOptions = new(JsonSerializerDefaults.Web);
        private static readonly TimeSpan PendingLifetime = TimeSpan.FromHours(4);

        private readonly StockMindDbContext _dbContext;
        private readonly CustomerRepository _customerRepository;
        private readonly ILogger<SalesOrderService> _logger;

        public SalesOrderService(
            StockMindDbContext dbContext,
            CustomerRepository customerRepository,
            ILogger<SalesOrderService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PageResponseModel<SalesOrderCatalogProductDto>> SearchCatalogAsync(
            SalesOrderCatalogQueryDto query,
            CancellationToken cancellationToken)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            query.Normalize();
            cancellationToken.ThrowIfCancellationRequested();

            var baseQuery = ApplyFilters(_dbContext.Products!, query);
            var total = await baseQuery.LongCountAsync(cancellationToken);

            var skip = (query.PageNum - 1) * query.PageSize;
            var products = await baseQuery
                .Include(p => p.Category)!
                    .ThenInclude(c => c!.ParentCategory)
                .Include(p => p.Supplier)
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ThenBy(p => p.SkuCode)
                .Skip(skip)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            if (products.Count == 0)
            {
                return new PageResponseModel<SalesOrderCatalogProductDto>(
                    query.PageSize,
                    query.PageNum,
                    0,
                    Array.Empty<SalesOrderCatalogProductDto>());
            }

            var productIds = products.Select(product => product.ProductId).ToList();
            var lots = await _dbContext.Lots
                .AsNoTracking()
                .Where(lot => !lot.Deleted && lot.QtyOnHand > 0 && productIds.Contains(lot.ProductId))
                .ToListAsync(cancellationToken);

            var lotIds = lots.Select(lot => lot.LotId).ToList();
            var discountMap = await LoadDiscountMapAsync(lotIds, cancellationToken);
            var lotCostMap = await LoadLotCostMapAsync(lotIds, cancellationToken);
            var suggestionsMap = await LoadReplenishmentSuggestionsAsync(productIds, cancellationToken);

            var categoryKeys = products
                .Select(product => product.Category?.ParentCategoryId ?? product.CategoryId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var marginMap = await _dbContext.MarginProfiles
                .AsNoTracking()
                .Where(profile => !profile.Deleted && categoryKeys.Contains(profile.ParentCategoryId))
                .ToDictionaryAsync(profile => profile.ParentCategoryId, cancellationToken);

            var lotsByProduct = lots
                .GroupBy(lot => lot.ProductId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var productDtos = new List<SalesOrderCatalogProductDto>(products.Count);
            foreach (var product in products)
            {
                lotsByProduct.TryGetValue(product.ProductId, out var productLots);
                var parentCategoryId = product.Category?.ParentCategoryId ?? product.CategoryId;
                marginMap.TryGetValue(parentCategoryId ?? -1, out var marginProfile);
                var lotDtos = BuildLotDtos(productLots, discountMap, lotCostMap, marginProfile);
                var displayPrice = lotDtos.FirstOrDefault()?.ComputedPrice ?? product.Price;
                suggestionsMap.TryGetValue(product.ProductId, out var suggestion);

                productDtos.Add(new SalesOrderCatalogProductDto
                {
                    ProductId = product.ProductId,
                    SkuCode = product.SkuCode,
                    Name = product.Name,
                    Uom = product.Uom,
                    Price = displayPrice,
                    IsPerishable = product.IsPerishable,
                    MediaUrl = product.MediaUrl,
                    SupplierName = product.Supplier?.Name,
                    SupplierId = product.SupplierId,
                    CategoryName = product.Category?.Name,
                    CategoryId = product.CategoryId,
                    ParentCategoryName = product.Category?.ParentCategory?.Name,
                    ParentCategoryId = product.Category?.ParentCategoryId,
                    OnHand = lotDtos.Sum(lot => lot.QtyOnHand),
                    MinStock = product.MinStock,
                    SuggestedQty = suggestion?.SuggestedQty,
                    MarginFloorPercent = marginProfile?.MinMarginPct,
                    TargetMarginPercent = marginProfile?.TargetMarginPct,
                    Lots = lotDtos
                });
            }

            var safeTotal = total > int.MaxValue ? int.MaxValue : (int)total;
            return new PageResponseModel<SalesOrderCatalogProductDto>(
                query.PageSize,
                query.PageNum,
                safeTotal,
                productDtos);
        }

        public async Task<SalesOrderSeedDto> GenerateSeedAsync(CancellationToken cancellationToken)
        {
            var code = await GenerateOrderCodeAsync(null, cancellationToken);
            return new SalesOrderSeedDto
            {
                OrderCode = code,
                GeneratedAt = DateTime.UtcNow
            };
        }

        [Transactional]
        public async Task<SalesOrderResponseDto> CreateOrderAsync(
            CreateSalesOrderRequestDto request,
            long cashierId,
            CancellationToken cancellationToken)
        {
            var prepared = await PrepareOrderAsync(request, cashierId, cancellationToken);
            return await PersistOrderAsync(prepared, cancellationToken);
        }

        public async Task<PendingSalesOrderResponseDto> CreatePendingOrderAsync(
            CreateSalesOrderRequestDto request,
            long cashierId,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!request.CustomerId.HasValue)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "customerId" });
            }

            var prepared = await PrepareOrderAsync(request, cashierId, cancellationToken);
            if (string.IsNullOrWhiteSpace(prepared.CustomerEmail))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "customer email" });
            }

            var sanitizedRequest = new CreateSalesOrderRequestDto
            {
                DraftOrderCode = prepared.OrderCode,
                CustomerId = prepared.CustomerId,
                CashierNotes = prepared.CashierNotes,
                Lines = prepared.Lines
                    .Select(line => new SalesOrderLineRequestDto
                    {
                        LotId = line.LotId,
                        ProductId = line.ProductId,
                        Qty = line.Qty
                    })
                    .ToList()
            };

            var pending = new SalesOrderPending
            {
                CashierId = prepared.CashierId,
                CustomerId = prepared.CustomerId!.Value,
                PayloadJson = JsonSerializer.Serialize(sanitizedRequest, PendingSerializerOptions),
                ConfirmationToken = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.Add(PendingLifetime),
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.SalesOrderPendings.AddAsync(pending, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Pending sales order {OrderCode} created for customer {CustomerId}. Confirmation token: {Token}",
                prepared.OrderCode,
                prepared.CustomerId,
                pending.ConfirmationToken);

            return new PendingSalesOrderResponseDto
            {
                PendingId = pending.PendingId,
                ConfirmationToken = pending.ConfirmationToken,
                OrderCode = prepared.OrderCode,
                ExpiresAt = pending.ExpiresAt,
                Message = $"Confirmation email sent to {prepared.CustomerEmail}."
            };
        }

        [Transactional]
        public async Task<SalesOrderResponseDto> ConfirmPendingOrderAsync(string token, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token) || !Guid.TryParse(token, out var tokenGuid))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "token" });
            }

            var pending = await _dbContext.SalesOrderPendings
                .FirstOrDefaultAsync(entity => entity.ConfirmationToken == tokenGuid, cancellationToken)
                ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { "pending order" });

            if (pending.ExpiresAt < DateTime.UtcNow)
            {
                _dbContext.SalesOrderPendings.Remove(pending);
                await _dbContext.SaveChangesAsync(cancellationToken);
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "token expired" });
            }

            var request = JsonSerializer.Deserialize<CreateSalesOrderRequestDto>(pending.PayloadJson, PendingSerializerOptions)
                           ?? throw new BizException(ErrorCode4xx.InvalidInput, new[] { "pending payload" });

            var order = await CreateOrderAsync(request, pending.CashierId, cancellationToken);
            _dbContext.SalesOrderPendings.Remove(pending);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return order;
        }

        private async Task<PreparedOrder> PrepareOrderAsync(
            CreateSalesOrderRequestDto request,
            long cashierId,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Lines == null || request.Lines.Count == 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "lines" });
            }

            var normalizedLines = request.Lines
                .Select(line => new SalesOrderLineRequestDto
                {
                    LotId = line.LotId,
                    ProductId = line.ProductId,
                    Qty = decimal.Round(line.Qty, 4, MidpointRounding.AwayFromZero)
                })
                .ToList();

            if (normalizedLines.Any(line => line.Qty <= 0))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "qty" });
            }

            var lotIds = normalizedLines.Select(line => line.LotId).Distinct().ToList();
            if (lotIds.Count == 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "lotIds" });
            }

            var lots = await _dbContext.Lots
                .Include(lot => lot.Product)!
                    .ThenInclude(product => product.Category)
                        .ThenInclude(category => category!.ParentCategory)
                .Include(lot => lot.Product.Supplier)
                .Where(lot => lotIds.Contains(lot.LotId))
                .ToDictionaryAsync(lot => lot.LotId, cancellationToken);

            if (lots.Count != lotIds.Count)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { "lot" });
            }

            foreach (var lot in lots.Values)
            {
                if (lot.Deleted)
                {
                    throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { lot.LotCode });
                }
            }

            var totalQtyByLot = normalizedLines
                .GroupBy(line => line.LotId)
                .ToDictionary(group => group.Key, group => group.Sum(line => line.Qty));

            foreach (var (lotId, requestedQty) in totalQtyByLot)
            {
                var lot = lots[lotId];
                if (requestedQty > lot.QtyOnHand)
                {
                    throw new BizException(ErrorCode4xx.InvalidInput, new[] { $"lot:{lot.LotCode}" });
                }
            }

            var customerId = request.CustomerId.HasValue && request.CustomerId.Value > 0 ? request.CustomerId : null;

            Customer? customer = null;
            if (customerId.HasValue)
            {
                customer = await _customerRepository.FindByIdAsync(customerId.Value, cancellationToken)
                           ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { "customer" });
            }

            var lotCostMap = await LoadLotCostMapAsync(lotIds, cancellationToken);
            var discountMap = await LoadDiscountMapAsync(lotIds, cancellationToken);

            var marginKeys = lots.Values
                .Select(lot => lot.Product.Category?.ParentCategoryId ?? lot.Product.CategoryId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var marginMap = await _dbContext.MarginProfiles
                .AsNoTracking()
                .Where(profile => !profile.Deleted && marginKeys.Contains(profile.ParentCategoryId))
                .ToDictionaryAsync(profile => profile.ParentCategoryId, cancellationToken);

            var orderCode = await GenerateOrderCodeAsync(request.DraftOrderCode, cancellationToken);
            var notes = string.IsNullOrWhiteSpace(request.CashierNotes) ? null : request.CashierNotes.Trim();
            var lines = new List<PreparedOrderLine>(normalizedLines.Count);
            decimal subtotal = 0;
            decimal discountTotal = 0;

            foreach (var line in normalizedLines)
            {
                var lot = lots[line.LotId];
                if (lot.ProductId != line.ProductId)
                {
                    throw new BizException(ErrorCode4xx.InvalidInput, new[] { $"product:{line.ProductId}" });
                }
                var marginKey = lot.Product.Category?.ParentCategoryId ?? lot.Product.CategoryId;
                marginMap.TryGetValue(marginKey ?? -1, out var marginProfile);
                var unitPrice = ResolveUnitPrice(lot, lotCostMap, marginProfile);
                var lineSubtotal = decimal.Round(line.Qty * unitPrice, 4, MidpointRounding.AwayFromZero);
                var discountPercent = discountMap.TryGetValue(line.LotId, out var discount) ? discount : 0m;
                var lineDiscount = discountPercent > 0
                    ? decimal.Round(lineSubtotal * discountPercent, 4, MidpointRounding.AwayFromZero)
                    : 0m;
                var lineTotal = lineSubtotal - lineDiscount;

                subtotal += lineSubtotal;
                discountTotal += lineDiscount;

                lines.Add(new PreparedOrderLine
                {
                    LotId = line.LotId,
                    ProductId = line.ProductId,
                    Qty = line.Qty,
                    UnitPrice = unitPrice,
                    AppliedDiscountPercent = discountPercent > 0 ? discountPercent : null,
                    LineSubtotal = lineSubtotal,
                    LineTotal = lineTotal,
                    IsWeightBased = IsWeightBased(lot.Product)
                });
            }

            var prepared = new PreparedOrder
            {
                OrderCode = orderCode,
                CashierId = cashierId,
                CustomerId = customerId,
                CustomerEmail = customer?.Email,
                CashierNotes = notes,
                Lines = lines,
                Subtotal = subtotal,
                DiscountTotal = discountTotal,
                Total = subtotal - discountTotal,
                Lots = lots,
                QtyByLot = totalQtyByLot
            };

            return prepared;
        }

        private async Task<SalesOrderResponseDto> PersistOrderAsync(PreparedOrder prepared, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            foreach (var (lotId, qty) in prepared.QtyByLot)
            {
                var affected = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE dbo.Lot SET qty_on_hand = qty_on_hand - {qty}, last_modified_at = SYSDATETIME() WHERE lot_id = {lotId} AND qty_on_hand >= {qty}",
                    cancellationToken);

                if (affected == 0)
                {
                    throw new BizException(ErrorCode4xx.InvalidInput, new[] { $"lot:{lotId}" });
                }
            }

            var orderItems = prepared.Lines.Select(line => new SalesOrderItem
            {
                ProductId = line.ProductId,
                LotId = line.LotId,
                Qty = line.Qty,
                UnitPrice = line.UnitPrice,
                AppliedMarkdownPercent = line.AppliedDiscountPercent,
                LineSubtotal = line.LineSubtotal,
                LineTotal = line.LineTotal,
                IsWeightBased = line.IsWeightBased,
                CreatedAt = now,
                LastModifiedAt = now,
                Deleted = false
            }).ToList();

            var order = new SalesOrder
            {
                OrderCode = prepared.OrderCode,
                CashierId = prepared.CashierId,
                CustomerId = prepared.CustomerId,
                CashierNotes = prepared.CashierNotes,
                ItemsCount = orderItems.Count,
                Subtotal = prepared.Subtotal,
                DiscountTotal = prepared.DiscountTotal,
                Total = prepared.Total,
                CreatedAt = now,
                LastModifiedAt = now,
                Deleted = false,
                SalesOrderItems = orderItems
            };

            await _dbContext.SalesOrders.AddAsync(order, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var movements = prepared.Lines.Select(line => new StockMovement
            {
                ProductId = line.ProductId,
                LotId = line.LotId,
                Qty = -line.Qty,
                Type = "OUT_SALE",
                RefType = "SALES_ORDER",
                RefId = order.OrderId,
                ActorId = prepared.CashierId,
                Reason = $"Sale {order.OrderCode}",
                CreatedAt = now
            }).ToList();

            await _dbContext.StockMovements.AddRangeAsync(movements, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Sales order {OrderCode} created with {LineCount} lines.",
                order.OrderCode,
                order.ItemsCount);

            var responseLines = orderItems.Select(item =>
            {
                var lot = prepared.Lots[item.LotId!.Value];
                var product = lot.Product;
                return new SalesOrderLineResponseDto
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    SkuCode = product.SkuCode,
                    LotId = lot.LotId,
                    LotCode = lot.LotCode,
                    Qty = item.Qty,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = item.AppliedMarkdownPercent,
                    LineSubtotal = item.LineSubtotal,
                    LineTotal = item.LineTotal,
                    Uom = product.Uom
                };
            }).ToList();

            return new SalesOrderResponseDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                CreatedAt = order.CreatedAt,
                ItemsCount = order.ItemsCount,
                Subtotal = order.Subtotal,
                DiscountTotal = order.DiscountTotal,
                Total = order.Total,
                CashierId = order.CashierId,
                CustomerId = order.CustomerId,
                Lines = responseLines
            };
        }

        private static bool IsWeightBased(Product product)
        {
            var uom = product.Uom?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(uom))
            {
                return false;
            }

            return uom is "kg" or "kilogram" or "g" or "gram" or "lb" or "l" or "liter";
        }

        private static decimal ResolveUnitPrice(
            Lot lot,
            IReadOnlyDictionary<long, decimal> lotCostMap,
            MarginProfile? marginProfile)
        {
            var unitCost = lotCostMap.TryGetValue(lot.LotId, out var cost) ? cost : 0m;
            if (unitCost > 0 && marginProfile != null)
            {
                var computed = unitCost * (1 + marginProfile.TargetMarginPct);
                return decimal.Round(computed, 4, MidpointRounding.AwayFromZero);
            }

            return lot.Product.Price > 0
                ? lot.Product.Price
                : decimal.Round(unitCost, 4, MidpointRounding.AwayFromZero);
        }

        private static IReadOnlyList<SalesOrderCatalogLotDto> BuildLotDtos(
            IList<Lot>? productLots,
            IReadOnlyDictionary<long, decimal> discountMap,
            IReadOnlyDictionary<long, decimal> costMap,
            MarginProfile? marginProfile)
        {
            if (productLots == null || productLots.Count == 0)
            {
                return Array.Empty<SalesOrderCatalogLotDto>();
            }

            return productLots
                .OrderBy(lot => lot.ExpiryDate ?? DateOnly.MaxValue)
                .ThenBy(lot => lot.ReceivedAt)
                .Select(lot =>
                {
                    costMap.TryGetValue(lot.LotId, out var unitCost);
                    var computedPrice = unitCost > 0 && marginProfile != null
                        ? decimal.Round(unitCost * (1 + marginProfile.TargetMarginPct), 4, MidpointRounding.AwayFromZero)
                        : (lot.Product.Price > 0 ? lot.Product.Price : unitCost);

                    return new SalesOrderCatalogLotDto
                    {
                        LotId = lot.LotId,
                        LotCode = lot.LotCode,
                        ReceivedAt = lot.ReceivedAt,
                        ExpiryDate = lot.ExpiryDate?.ToDateTime(TimeOnly.MinValue),
                        QtyOnHand = lot.QtyOnHand,
                        DiscountPercent = discountMap.TryGetValue(lot.LotId, out var discount) ? discount : 0m,
                        UnitCost = unitCost > 0 ? unitCost : null,
                        ComputedPrice = computedPrice
                    };
                })
                .ToList();
        }

        private async Task<Dictionary<long, decimal>> LoadDiscountMapAsync(
            IReadOnlyCollection<long> lotIds,
            CancellationToken cancellationToken)
        {
            if (lotIds.Count == 0)
            {
                return new Dictionary<long, decimal>();
            }

            var decisions = await _dbContext.LotSaleDecisions
                .AsNoTracking()
                .Where(decision => !decision.Deleted && decision.IsApplied && lotIds.Contains(decision.LotId))
                .GroupBy(decision => decision.LotId)
                .Select(group => group.OrderByDescending(decision => decision.LastModifiedAt).First())
                .ToListAsync(cancellationToken);

            return decisions.ToDictionary(decision => decision.LotId, decision => decision.DiscountPercent);
        }

        private async Task<Dictionary<long, decimal>> LoadLotCostMapAsync(
            IReadOnlyCollection<long> lotIds,
            CancellationToken cancellationToken)
        {
            if (lotIds.Count == 0)
            {
                return new Dictionary<long, decimal>();
            }

            var entries = await _dbContext.Grnitems
                .AsNoTracking()
                .Where(item => item.LotId.HasValue && lotIds.Contains(item.LotId.Value))
                .GroupBy(item => item.LotId!.Value)
                .Select(group => group.OrderByDescending(item => item.CreatedAt).First())
                .ToListAsync(cancellationToken);

            return entries.ToDictionary(entry => entry.LotId!.Value, entry => entry.UnitCost);
        }

        private async Task<Dictionary<long, ReplenishmentSuggestion>> LoadReplenishmentSuggestionsAsync(
            IReadOnlyCollection<long> productIds,
            CancellationToken cancellationToken)
        {
            if (productIds.Count == 0)
            {
                return new Dictionary<long, ReplenishmentSuggestion>();
            }

            var suggestions = await _dbContext.ReplenishmentSuggestions
                .AsNoTracking()
                .Where(suggestion => !suggestion.Deleted && productIds.Contains(suggestion.ProductId))
                .GroupBy(suggestion => suggestion.ProductId)
                .Select(group => group.OrderByDescending(entry => entry.ComputedAt).First())
                .ToListAsync(cancellationToken);

            return suggestions.ToDictionary(entry => entry.ProductId, entry => entry);
        }

        private async Task<string> GenerateOrderCodeAsync(string? preferredCode, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(preferredCode))
            {
                var normalized = preferredCode.Trim().ToUpperInvariant();
                var exists = await _dbContext.SalesOrders.AnyAsync(order => order.OrderCode == normalized, cancellationToken);
                if (!exists)
                {
                    return normalized;
                }

                _logger.LogWarning("Provided order code {OrderCode} already exists. Generating a new code.", normalized);
            }

            for (var attempt = 0; attempt < 5; attempt++)
            {
                var candidate = BuildOrderCode();
                var exists = await _dbContext.SalesOrders.AnyAsync(order => order.OrderCode == candidate, cancellationToken);
                if (!exists)
                {
                    return candidate;
                }
            }

            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "orderCode" });
        }

        private static string BuildOrderCode()
        {
            Span<byte> buffer = stackalloc byte[4];
            RandomNumberGenerator.Fill(buffer);
            var suffix = BitConverter.ToUInt32(buffer) % 10000;
            return $"SO-{DateTime.UtcNow:yyyyMMddHHmmss}-{suffix:D4}";
        }

        private static IQueryable<Product> ApplyFilters(
            IQueryable<Product> query,
            SalesOrderCatalogQueryDto criteria)
        {
            query = query.Where(product => !product.Deleted && product.Lots.Any(lot => !lot.Deleted && lot.QtyOnHand > 0));

            if (!string.IsNullOrWhiteSpace(criteria.Search))
            {
                var like = $"%{criteria.Search}%";
                query = query.Where(product => EF.Functions.Like(product.Name, like) || EF.Functions.Like(product.SkuCode, like));
            }

            if (criteria.CategoryIdList.Count > 0)
            {
                var set = criteria.CategoryIdList.ToHashSet();
                query = query.Where(product => product.CategoryId.HasValue && set.Contains(product.CategoryId.Value));
            }

            if (criteria.ParentCategoryIdList.Count > 0)
            {
                var parentSet = criteria.ParentCategoryIdList.ToHashSet();
                query = query.Where(product =>
                    (product.Category != null && product.Category.ParentCategoryId.HasValue && parentSet.Contains(product.Category.ParentCategoryId.Value)) ||
                    (product.CategoryId.HasValue && parentSet.Contains(product.CategoryId.Value)));
            }

            if (criteria.SupplierIdList.Count > 0)
            {
                var supplierSet = criteria.SupplierIdList.ToHashSet();
                query = query.Where(product => product.SupplierId.HasValue && supplierSet.Contains(product.SupplierId.Value));
            }

            return query;
        }

        private sealed class PreparedOrder
        {
            public string OrderCode { get; init; } = string.Empty;
            public long CashierId { get; init; }
            public long? CustomerId { get; init; }
            public string? CustomerEmail { get; init; }
            public string? CashierNotes { get; init; }
            public List<PreparedOrderLine> Lines { get; init; } = new();
            public decimal Subtotal { get; init; }
            public decimal DiscountTotal { get; init; }
            public decimal Total { get; init; }
            public IDictionary<long, Lot> Lots { get; init; } = new Dictionary<long, Lot>();
            public IDictionary<long, decimal> QtyByLot { get; init; } = new Dictionary<long, decimal>();
        }

        private sealed class PreparedOrderLine
        {
            public long ProductId { get; init; }
            public long LotId { get; init; }
            public decimal Qty { get; init; }
            public decimal UnitPrice { get; init; }
            public decimal? AppliedDiscountPercent { get; init; }
            public decimal LineSubtotal { get; init; }
            public decimal LineTotal { get; init; }
            public bool IsWeightBased { get; init; }
        }
    }
}
