using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using stockmind.Commons.Attributes;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.DTOs.SalesOrders;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services;

public class SalesOrderService
{
    private const string SearchCollation = "SQL_Latin1_General_CP1_CI_AI";
    private const int LoyaltyRedemptionStep = 1000;
    private const decimal LoyaltyValuePerStep = 1000m;
    private const decimal LoyaltyEarnDivisor = 100m;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly SalesOrderRepository _salesOrderRepository;
    private readonly LotRepository _lotRepository;
    private readonly LotSaleDecisionRepository _lotSaleDecisionRepository;
    private readonly CustomerService _customerService;
    private readonly StockMovementRepository _stockMovementRepository;
    private readonly EmailService _emailService;
    private readonly StockMindDbContext _dbContext;
    private readonly ILogger<SalesOrderService> _logger;

    public SalesOrderService(
        SalesOrderRepository salesOrderRepository,
        LotRepository lotRepository,
        LotSaleDecisionRepository lotSaleDecisionRepository,
        CustomerService customerService,
        StockMovementRepository stockMovementRepository,
        EmailService emailService,
        StockMindDbContext dbContext,
        ILogger<SalesOrderService> logger)
    {
        _salesOrderRepository = salesOrderRepository;
        _lotRepository = lotRepository;
        _lotSaleDecisionRepository = lotSaleDecisionRepository;
        _customerService = customerService;
        _stockMovementRepository = stockMovementRepository;
        _emailService = emailService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<SalesOrderContextDto> GetContextAsync(long cashierId, string? cashierName, CancellationToken cancellationToken)
    {
        var context = new SalesOrderContextDto
        {
            CashierId = cashierId,
            CashierName = string.IsNullOrWhiteSpace(cashierName) ? "Cashier" : cashierName!,
            OrderCode = GenerateOrderCode(),
            GeneratedAt = DateTime.UtcNow,
            LoyaltyRedemptionStep = LoyaltyRedemptionStep,
            LoyaltyValuePerStep = LoyaltyValuePerStep,
            LoyaltyEarnRate = 1m / LoyaltyEarnDivisor
        };

        return Task.FromResult(context);
    }

    public async Task<IReadOnlyList<SellableLotDto>> SearchSellableLotsAsync(
        SellableLotQueryDto query,
        CancellationToken cancellationToken)
    {
        var sanitized = NormalizeQuery(query);
        var limit = Math.Clamp(sanitized.Limit, 1, 100);

        var lotQuery = _lotRepository.Query()
            .Include(lot => lot.Product!)
                .ThenInclude(product => product.Category!)
                    .ThenInclude(category => category.MarginProfile)
            .Include(lot => lot.Product!)
                .ThenInclude(product => product.Category!)
                    .ThenInclude(category => category.ParentCategory)
                        .ThenInclude(parent => parent!.MarginProfile)
            .Include(lot => lot.Product!)
                .ThenInclude(product => product.Supplier)
            .Include(lot => lot.Grnitems)
            .Where(lot => !lot.Deleted && !lot.Product.Deleted)
            .Where(lot => lot.QtyOnHand > 0);

        if (!string.IsNullOrWhiteSpace(sanitized.SearchTerm))
        {
            var keyword = sanitized.SearchTerm!;
            var likePattern = $"%{EscapeLikePattern(keyword)}%";
            lotQuery = lotQuery.Where(lot =>
                EF.Functions.Like(EF.Functions.Collate(lot.Product.Name, SearchCollation), likePattern) ||
                EF.Functions.Like(EF.Functions.Collate(lot.Product.SkuCode, SearchCollation), likePattern));
        }

        if (sanitized.CategoryIds.Count > 0)
        {
            lotQuery = lotQuery.Where(lot =>
                lot.Product.CategoryId.HasValue && sanitized.CategoryIds.Contains(lot.Product.CategoryId.Value));
        }

        if (sanitized.ParentCategoryIds.Count > 0)
        {
            lotQuery = lotQuery.Where(lot =>
                lot.Product.Category != null &&
                lot.Product.Category.ParentCategoryId.HasValue &&
                sanitized.ParentCategoryIds.Contains(lot.Product.Category.ParentCategoryId.Value));
        }

        if (sanitized.SupplierIds.Count > 0)
        {
            lotQuery = lotQuery.Where(lot =>
                lot.Product.SupplierId.HasValue &&
                sanitized.SupplierIds.Contains(lot.Product.SupplierId.Value));
        }

        var lots = await lotQuery
            .OrderBy(lot => lot.ExpiryDate ?? DateOnly.MaxValue)
            .ThenBy(lot => lot.Product.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (lots.Count == 0)
        {
            return Array.Empty<SellableLotDto>();
        }

        var lotIds = lots.Select(lot => lot.LotId).ToList();
        var lotDecisionLookup = await _lotSaleDecisionRepository.GetLatestByLotIdsAsync(lotIds, cancellationToken);

        var productIds = lots.Select(lot => lot.ProductId).Distinct().ToList();
        var replenishmentLookup = await LoadReplenishmentLookupAsync(productIds, cancellationToken);

        var result = new List<SellableLotDto>(lots.Count);
        foreach (var lot in lots)
        {
            var product = lot.Product;
            var (targetMarginPct, minMarginPct) = ResolveMargins(product);
            var unitCost = ResolveUnitCost(lot);
            var unitPrice = CalculateUnitPrice(unitCost, targetMarginPct);
            var hasPricingGaps = unitCost <= 0 || targetMarginPct <= 0;

            lotDecisionLookup.TryGetValue(lot.LotId, out var decision);
            replenishmentLookup.TryGetValue(lot.ProductId, out var replenishment);

            result.Add(new SellableLotDto
            {
                LotId = lot.LotId,
                LotCode = lot.LotCode,
                ProductId = product.ProductId,
                ProductName = product.Name,
                SkuCode = product.SkuCode,
                Uom = product.Uom,
                QtyOnHand = lot.QtyOnHand,
                ExpiryDate = lot.ExpiryDate?.ToDateTime(TimeOnly.MinValue),
                ReceivedAt = lot.ReceivedAt,
                MediaUrl = product.MediaUrl,
                SupplierName = product.Supplier?.Name,
                CategoryName = product.Category?.Name,
                ParentCategoryName = product.Category?.ParentCategory?.Name,
                IsPerishable = product.IsPerishable,
                UnitCost = unitCost,
                TargetMarginPct = targetMarginPct,
                MinMarginPct = minMarginPct,
                UnitPrice = unitPrice,
                DiscountPercent = decision is { IsApplied: true } ? decision.DiscountPercent : 0m,
                SuggestedQty = replenishment?.SuggestedQty,
                HasPricingGaps = hasPricingGaps
            });
        }

        return result;
    }

    [Transactional]
    public async Task<CreateSalesOrderResponseDto> CreateSalesOrderAsync(
        CreateSalesOrderRequestDto request,
        long cashierId,
        string? cashierName,
        string confirmationBaseUrl,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var draft = await BuildOrderDraftAsync(request, cancellationToken);
        Customer? customer = null;
        if (request.CustomerId.HasValue)
        {
            customer = await _customerService.GetByIdAsync(request.CustomerId.Value, cancellationToken);
        }

        ApplyLoyaltyAdjustments(draft, customer, request.LoyaltyPointsToRedeem);

        var requiresConfirmation = customer != null;
        if (requiresConfirmation)
        {
            if (string.IsNullOrWhiteSpace(customer!.Email))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "customerEmailRequired" });
            }

            var pending = await CreatePendingOrderAsync(
                request,
                draft,
                customer,
                cashierId,
                cashierName,
                confirmationBaseUrl,
                cancellationToken);

            return new CreateSalesOrderResponseDto
            {
                Status = "PENDING",
                Pending = pending
            };
        }

        var summary = await PersistOrderAsync(draft, customer, cashierId, cashierName, cancellationToken);

        return new CreateSalesOrderResponseDto
        {
            Status = "CONFIRMED",
            Order = summary
        };
    }

    [Transactional]
    public async Task<SalesOrderSummaryDto> ConfirmPendingOrderAsync(Guid token, CancellationToken cancellationToken)
    {
        var pending = await _salesOrderRepository.GetPendingByTokenAsync(token, cancellationToken)
                      ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { "pendingSalesOrder" });

        if (!string.Equals(pending.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "pendingAlreadyProcessed" });
        }

        if (pending.ExpiresAt < DateTime.UtcNow)
        {
            pending.Status = "EXPIRED";
            await _salesOrderRepository.UpdatePendingAsync(pending, cancellationToken);
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "pendingExpired" });
        }

        var payload = JsonSerializer.Deserialize<PendingSalesOrderPayload>(pending.PayloadJson, SerializerOptions)
                      ?? throw new BizException(ErrorCode4xx.InvalidInput, new[] { "pendingPayloadInvalid" });

        var request = new CreateSalesOrderRequestDto
        {
            OrderCode = payload.OrderCode,
            CustomerId = payload.CustomerId,
            LoyaltyPointsToRedeem = payload.LoyaltyPointsToRedeem,
            Lines = payload.Lines
        };

        var draft = await BuildOrderDraftAsync(request, cancellationToken);
        var customer = await _customerService.GetByIdAsync(payload.CustomerId, cancellationToken);

        ApplyLoyaltyAdjustments(draft, customer, request.LoyaltyPointsToRedeem);

        var summary = await PersistOrderAsync(draft, customer, payload.CashierId, payload.CashierName, cancellationToken);

        pending.Status = "CONFIRMED";
        pending.ConfirmedAt = DateTime.UtcNow;
        await _salesOrderRepository.UpdatePendingAsync(pending, cancellationToken);

        return summary;
    }

    private async Task<OrderDraft> BuildOrderDraftAsync(CreateSalesOrderRequestDto request, CancellationToken cancellationToken)
    {
        if (request.Lines == null || request.Lines.Count == 0)
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "lines" });
        }

        var lotIds = request.Lines.Select(line => line.LotId).Distinct().ToList();
        var lots = await _lotRepository.Query()
            .Include(lot => lot.Product!)
                .ThenInclude(product => product.Category!)
                    .ThenInclude(category => category.MarginProfile)
            .Include(lot => lot.Product!)
                .ThenInclude(product => product.Category!)
                    .ThenInclude(category => category.ParentCategory)
                        .ThenInclude(parent => parent!.MarginProfile)
            .Include(lot => lot.Product!)
                .ThenInclude(product => product.Supplier)
            .Include(lot => lot.Grnitems)
            .Where(lot => lotIds.Contains(lot.LotId))
            .ToListAsync(cancellationToken);

        if (lots.Count != lotIds.Count)
        {
            throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { "lot" });
        }

        var lotDecisionLookup = await _lotSaleDecisionRepository.GetLatestByLotIdsAsync(lotIds, cancellationToken);

        var incomingCode = string.IsNullOrWhiteSpace(request.OrderCode) ? null : request.OrderCode!.Trim();
        var orderCode = incomingCode;
        if (string.IsNullOrEmpty(orderCode) || await _salesOrderRepository.ExistsByCodeAsync(orderCode, cancellationToken))
        {
            orderCode = GenerateOrderCode();
        }

        var draft = new OrderDraft
        {
            OrderCode = orderCode
        };

        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "quantity" });
            }

            var lot = lots.First(l => l.LotId == line.LotId);
            if (lot.ProductId != line.ProductId)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "productMismatch" });
            }

            if (line.Quantity > lot.QtyOnHand)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { $"qty_on_hand_{lot.LotCode}" });
            }

            lotDecisionLookup.TryGetValue(lot.LotId, out var decision);

            var draftLine = BuildDraftLine(lot, line.Quantity, decision);

            draft.Lines.Add(draftLine);
            draft.Subtotal += draftLine.LineSubtotal;
            draft.DiscountTotal += draftLine.LineDiscount;
        }

        draft.ItemsCount = draft.Lines.Count;
        draft.Total = decimal.Round(draft.Subtotal - draft.DiscountTotal, 0, MidpointRounding.AwayFromZero);
        draft.Total = Math.Max(draft.Total, 0);

        return draft;
    }

    private static OrderDraftLine BuildDraftLine(Lot lot, decimal qty, LotSaleDecision? decision)
    {
        var product = lot.Product;
        var (targetMarginPct, _) = ResolveMargins(product);
        var unitCost = ResolveUnitCost(lot);
        var unitPrice = CalculateUnitPrice(unitCost, targetMarginPct);
        var discount = decision is { IsApplied: true } ? decision.DiscountPercent : 0m;

        var subtotal = decimal.Round(unitPrice * qty, 0, MidpointRounding.AwayFromZero);
        var lineDiscount = discount <= 0 ? 0 : decimal.Round(subtotal * discount, 0, MidpointRounding.AwayFromZero);
        var lineTotal = Math.Max(0, subtotal - lineDiscount);

        return new OrderDraftLine
        {
            ProductId = product.ProductId,
            LotId = lot.LotId,
            LotCode = lot.LotCode,
            ProductName = product.Name,
            Uom = product.Uom,
            Quantity = qty,
            UnitPrice = unitPrice,
            DiscountPercent = discount,
            LineSubtotal = subtotal,
            LineDiscount = lineDiscount,
            LineTotal = lineTotal,
            IsWeightBased = string.Equals(product.Uom, "KG", StringComparison.OrdinalIgnoreCase),
            Lot = lot
        };
    }

    private void ApplyLoyaltyAdjustments(OrderDraft draft, Customer? customer, int loyaltyPointsToRedeem)
    {
        if (loyaltyPointsToRedeem <= 0)
        {
            draft.LoyaltyPointsRedeemed = 0;
            draft.LoyaltyAmountRedeemed = 0;
            draft.LoyaltyPointsEarned = (int)Math.Floor(draft.Total / LoyaltyEarnDivisor);
            return;
        }

        if (customer == null)
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "customerRequired" });
        }

        if (loyaltyPointsToRedeem % LoyaltyRedemptionStep != 0)
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "loyaltyStep" });
        }

        if (loyaltyPointsToRedeem > customer.LoyaltyPoints)
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "loyaltyInsufficient" });
        }

        var steps = loyaltyPointsToRedeem / LoyaltyRedemptionStep;
        var loyaltyAmount = steps * LoyaltyValuePerStep;
        if (loyaltyAmount > draft.Total)
        {
            loyaltyAmount = draft.Total;
            loyaltyPointsToRedeem = (int)loyaltyAmount;
        }

        draft.LoyaltyPointsRedeemed = loyaltyPointsToRedeem;
        draft.LoyaltyAmountRedeemed = loyaltyAmount;
        draft.Total = Math.Max(draft.Total - loyaltyAmount, 0);
        draft.LoyaltyPointsEarned = (int)Math.Floor(draft.Total / LoyaltyEarnDivisor);
    }

    private async Task<SalesOrderSummaryDto> PersistOrderAsync(
        OrderDraft draft,
        Customer? customer,
        long cashierId,
        string? cashierName,
        CancellationToken cancellationToken)
    {
        var order = new SalesOrder
        {
            OrderCode = draft.OrderCode,
            CashierId = cashierId,
            CustomerId = customer?.CustomerId,
            ItemsCount = draft.ItemsCount,
            Subtotal = draft.Subtotal,
            DiscountTotal = draft.DiscountTotal,
            Total = draft.Total,
            LoyaltyAmountRedeemed = draft.LoyaltyAmountRedeemed,
            LoyaltyPointsEarned = draft.LoyaltyPointsEarned,
            LoyaltyPointsRedeemed = draft.LoyaltyPointsRedeemed,
            CashierNotes = null,
            Deleted = false
        };

        foreach (var line in draft.Lines)
        {
            order.SalesOrderItems.Add(new SalesOrderItem
            {
                ProductId = line.ProductId,
                LotId = line.LotId,
                Qty = line.Quantity,
                UnitPrice = line.UnitPrice,
                AppliedMarkdownPercent = line.DiscountPercent,
                LineSubtotal = line.LineSubtotal,
                LineTotal = line.LineTotal,
                IsWeightBased = line.IsWeightBased,
                Deleted = false
            });
        }

        var persisted = await _salesOrderRepository.AddAsync(order, cancellationToken);

        if (customer != null)
        {
            customer.LoyaltyPoints = customer.LoyaltyPoints - draft.LoyaltyPointsRedeemed + draft.LoyaltyPointsEarned;
            await _customerService.UpdateAsync(customer, cancellationToken);
        }

        foreach (var line in draft.Lines)
        {
            line.Lot.QtyOnHand -= line.Quantity;
            if (line.Lot.QtyOnHand < 0)
            {
                line.Lot.QtyOnHand = 0;
            }
            line.Lot.LastModifiedAt = DateTime.UtcNow;
            await _lotRepository.UpdateAsync(line.Lot, cancellationToken);

            var movement = new StockMovement
            {
                ProductId = line.ProductId,
                LotId = line.LotId,
                Qty = -line.Quantity,
                Type = "OUT_SALE",
                RefType = "ORDER",
                RefId = persisted.OrderId,
                ActorId = cashierId,
                Reason = $"Sales order {persisted.OrderCode}"
            };

            await _stockMovementRepository.AddAsync(movement, cancellationToken);
        }

        _logger.LogInformation("Created sales order {OrderCode} ({OrderId})", persisted.OrderCode, persisted.OrderId);

        return BuildOrderSummaryDto(persisted, draft, cashierName, customer);
    }

    private async Task<PendingSalesOrderDto> CreatePendingOrderAsync(
        CreateSalesOrderRequestDto request,
        OrderDraft draft,
        Customer customer,
        long cashierId,
        string? cashierName,
        string confirmationBaseUrl,
        CancellationToken cancellationToken)
    {
        var payload = new PendingSalesOrderPayload
        {
            OrderCode = draft.OrderCode,
            CashierId = cashierId,
            CashierName = cashierName,
            CustomerId = customer.CustomerId,
            LoyaltyPointsToRedeem = request.LoyaltyPointsToRedeem,
            Lines = request.Lines
                .Select(line => new CreateSalesOrderLineDto
                {
                    ProductId = line.ProductId,
                    LotId = line.LotId,
                    Quantity = line.Quantity
                })
                .ToList(),
            RequestedAt = DateTime.UtcNow
        };

        var entity = new SalesOrderPending
        {
            CashierId = cashierId,
            CustomerId = customer.CustomerId,
            PayloadJson = JsonSerializer.Serialize(payload, SerializerOptions),
            ConfirmationToken = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Status = "PENDING"
        };

        var pending = await _salesOrderRepository.AddPendingAsync(entity, cancellationToken);

        var confirmationUrl = BuildConfirmationUrl(confirmationBaseUrl, pending.ConfirmationToken);
        await SendConfirmationEmailAsync(customer.Email!, draft, confirmationUrl, cancellationToken);

        return new PendingSalesOrderDto
        {
            PendingId = pending.PendingId,
            ConfirmationToken = pending.ConfirmationToken,
            ExpiresAt = pending.ExpiresAt,
            CustomerEmail = customer.Email
        };
    }

    private async Task SendConfirmationEmailAsync(string recipientEmail, OrderDraft draft, string confirmationUrl, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<p>Dear customer,</p>");
        sb.AppendLine("<p>Please confirm your sales order to redeem your loyalty points:</p>");
        sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;\">");
        sb.AppendLine("<tr><th align=\"left\">Product</th><th align=\"right\">Qty</th><th align=\"right\">Unit Price</th><th align=\"right\">Discount</th><th align=\"right\">Line Total</th></tr>");

        foreach (var line in draft.Lines)
        {
            sb.AppendLine("<tr>");
            sb.AppendFormat("<td>{0}</td>", line.ProductName);
            sb.AppendFormat("<td align=\"right\">{0}</td>", line.Quantity);
            sb.AppendFormat("<td align=\"right\">{0:N0}</td>", line.UnitPrice);
            sb.AppendFormat("<td align=\"right\">{0:P0}</td>", line.DiscountPercent);
            sb.AppendFormat("<td align=\"right\">{0:N0}</td>", line.LineTotal);
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
        sb.AppendFormat("<p>Subtotal: <strong>{0:N0} VND</strong></p>", draft.Subtotal);
        sb.AppendFormat("<p>Total Discount: <strong>{0:N0} VND</strong></p>", draft.DiscountTotal);
        sb.AppendFormat("<p>Loyalty Redemption: <strong>{0:N0} VND</strong></p>", draft.LoyaltyAmountRedeemed);
        sb.AppendFormat("<p>Final Total: <strong>{0:N0} VND</strong></p>", draft.Total);
        sb.AppendFormat("<p><a href=\"{0}\">Click here to confirm your order</a></p>", confirmationUrl);

        await _emailService.SendEmailAsync(
            recipientEmail,
            $"Confirm sales order {draft.OrderCode}",
            sb.ToString(),
            isBodyHtml: true,
            cancellationToken: cancellationToken);
    }

    private static SellableLotQueryDto NormalizeQuery(SellableLotQueryDto query)
    {
        return new SellableLotQueryDto
        {
            SearchTerm = query.SearchTerm?.Trim(),
            Limit = query.Limit,
            CategoryIds = query.CategoryIds?.Where(id => id > 0).Distinct().ToList() ?? new List<long>(),
            ParentCategoryIds = query.ParentCategoryIds?.Where(id => id > 0).Distinct().ToList() ?? new List<long>(),
            SupplierIds = query.SupplierIds?.Where(id => id > 0).Distinct().ToList() ?? new List<long>()
        };
    }

    private static decimal ResolveUnitCost(Lot lot)
    {
        var lastGrn = lot.Grnitems
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();

        return lastGrn?.UnitCost ?? 0m;
    }

    private static decimal CalculateUnitPrice(decimal unitCost, decimal targetMarginPct)
    {
        var marginRate = NormalizeMarginRate(targetMarginPct);
        if (unitCost <= 0)
        {
            return 0;
        }

        var price = unitCost / (1 - marginRate);
        return decimal.Round(price, 0, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizeMarginRate(decimal rawValue)
    {
        if (rawValue <= 0)
        {
            return 0;
        }

        return rawValue > 1 ? rawValue / 100m : rawValue;
    }

    private static (decimal target, decimal min) ResolveMargins(Product product)
    {
        decimal? target = product.Category?.MarginProfile?.TargetMarginPct;
        decimal? min = product.Category?.MarginProfile?.MinMarginPct;

        if ((!target.HasValue || target.Value <= 0) && product.Category?.ParentCategory?.MarginProfile != null)
        {
            target = product.Category.ParentCategory.MarginProfile.TargetMarginPct;
            min = product.Category.ParentCategory.MarginProfile.MinMarginPct;
        }

        return (target ?? 0m, min ?? 0m);
    }

    private async Task<Dictionary<long, ReplenishmentSuggestion>> LoadReplenishmentLookupAsync(
        IReadOnlyCollection<long> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<long, ReplenishmentSuggestion>();
        }

        var records = await _dbContext.ReplenishmentSuggestions
            .AsNoTracking()
            .Where(suggestion => !suggestion.Deleted && productIds.Contains(suggestion.ProductId))
            .OrderByDescending(suggestion => suggestion.LastModifiedAt)
            .ToListAsync(cancellationToken);

        return records
            .GroupBy(record => record.ProductId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static SalesOrderSummaryDto BuildOrderSummaryDto(
        SalesOrder order,
        OrderDraft draft,
        string? cashierName,
        Customer? customer)
    {
        var lines = draft.Lines
            .Select(line => new SalesOrderLineSummaryDto
            {
                ProductId = line.ProductId,
                LotId = line.LotId,
                ProductName = line.ProductName,
                LotCode = line.LotCode,
                Uom = line.Uom,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                DiscountPercent = line.DiscountPercent,
                LineTotal = line.LineTotal
            })
            .ToList();

        return new SalesOrderSummaryDto
        {
            OrderId = order.OrderId.ToString(),
            OrderCode = order.OrderCode,
            CreatedAt = order.CreatedAt,
            CashierName = cashierName ?? "Cashier",
            CustomerName = customer?.FullName,
            ItemsCount = order.ItemsCount,
            Subtotal = order.Subtotal,
            DiscountTotal = order.DiscountTotal,
            Total = order.Total,
            LoyaltyAmountRedeemed = order.LoyaltyAmountRedeemed,
            LoyaltyPointsEarned = order.LoyaltyPointsEarned,
            LoyaltyPointsRedeemed = order.LoyaltyPointsRedeemed,
            Lines = lines
        };
    }

    private static string BuildConfirmationUrl(string baseUrl, Guid token)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return $"{trimmed}/api/sales-orders/pending/{token}/confirm";
    }

    public async Task<PendingSalesOrderStatusDto> GetPendingStatusAsync(long pendingId, CancellationToken cancellationToken)
    {
        var pending = await _salesOrderRepository.GetPendingByIdAsync(pendingId, cancellationToken)
                      ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"pendingId={pendingId}" });

        return new PendingSalesOrderStatusDto
        {
            PendingId = pending.PendingId,
            Status = pending.Status,
            ExpiresAt = pending.ExpiresAt,
            ConfirmedAt = pending.ConfirmedAt
        };
    }

    public async Task CancelPendingOrderAsync(long pendingId, long cashierId, CancellationToken cancellationToken)
    {
        var pending = await _salesOrderRepository.GetPendingByIdAsync(pendingId, cancellationToken)
                      ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"pendingId={pendingId}" });

        if (!string.Equals(pending.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (pending.CashierId != cashierId)
        {
            throw new BizAuthorizationException(ErrorCode4xx.Forbidden, new[] { "pendingOrder" });
        }

        pending.Status = "CANCELLED";
        pending.ConfirmedAt = null;
        await _salesOrderRepository.CancelPendingAsync(pending, cancellationToken);
    }

    private static string EscapeLikePattern(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return input
            .Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
    }

    private static string GenerateOrderCode()
    {
        return $"SO-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    private sealed class OrderDraft
    {
        public string OrderCode { get; set; } = string.Empty;

        public List<OrderDraftLine> Lines { get; } = new();

        public int ItemsCount { get; set; }

        public decimal Subtotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal Total { get; set; }

        public int LoyaltyPointsRedeemed { get; set; }

        public decimal LoyaltyAmountRedeemed { get; set; }

        public int LoyaltyPointsEarned { get; set; }
    }

    private sealed class OrderDraftLine
    {
        public long ProductId { get; set; }

        public long LotId { get; set; }

        public string LotCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string Uom { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal LineSubtotal { get; set; }

        public decimal LineDiscount { get; set; }

        public decimal LineTotal { get; set; }

        public bool IsWeightBased { get; set; }

        public Lot Lot { get; set; } = null!;
    }
}
