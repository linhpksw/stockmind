namespace stockmind.DTOs.SalesOrders;

public class SalesOrderContextDto
{
    public string OrderCode { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    public long CashierId { get; set; }

    public string CashierName { get; set; } = string.Empty;

    public int LoyaltyRedemptionStep { get; set; }

    public decimal LoyaltyValuePerStep { get; set; }

    public decimal LoyaltyEarnRate { get; set; }
}
