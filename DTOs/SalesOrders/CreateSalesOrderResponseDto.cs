namespace stockmind.DTOs.SalesOrders;

public class CreateSalesOrderResponseDto
{
    public string Status { get; set; } = "CONFIRMED";

    public SalesOrderSummaryDto? Order { get; set; }

    public PendingSalesOrderDto? Pending { get; set; }
}

public class SalesOrderSummaryDto
{
    public string OrderId { get; set; } = string.Empty;

    public string OrderCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CashierName { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    public int ItemsCount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal Total { get; set; }

    public decimal LoyaltyAmountRedeemed { get; set; }

    public int LoyaltyPointsEarned { get; set; }

    public int LoyaltyPointsRedeemed { get; set; }

    public IReadOnlyCollection<SalesOrderLineSummaryDto> Lines { get; set; } = Array.Empty<SalesOrderLineSummaryDto>();
}

public class SalesOrderLineSummaryDto
{
    public long ProductId { get; set; }

    public long LotId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string LotCode { get; set; } = string.Empty;

    public string Uom { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal LineTotal { get; set; }
}

public class PendingSalesOrderDto
{
    public long PendingId { get; set; }

    public Guid ConfirmationToken { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string? CustomerEmail { get; set; }
}
