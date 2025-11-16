namespace stockmind.DTOs.SalesOrders;

public class PendingSalesOrderPayload
{
    public string OrderCode { get; set; } = string.Empty;

    public long CashierId { get; set; }

    public string? CashierName { get; set; }

    public long CustomerId { get; set; }

    public int LoyaltyPointsToRedeem { get; set; }

    public List<CreateSalesOrderLineDto> Lines { get; set; } = new();

    public DateTime RequestedAt { get; set; }
}
