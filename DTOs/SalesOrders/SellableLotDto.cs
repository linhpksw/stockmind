namespace stockmind.DTOs.SalesOrders;

public class SellableLotDto
{
    public long LotId { get; set; }

    public string LotCode { get; set; } = string.Empty;

    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SkuCode { get; set; } = string.Empty;

    public string Uom { get; set; } = string.Empty;

    public decimal QtyOnHand { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime ReceivedAt { get; set; }

    public string? MediaUrl { get; set; }

    public string? SupplierName { get; set; }

    public string? CategoryName { get; set; }

    public string? ParentCategoryName { get; set; }

    public bool IsPerishable { get; set; }

    public decimal UnitCost { get; set; }

    public decimal TargetMarginPct { get; set; }

    public decimal MinMarginPct { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal? SuggestedQty { get; set; }

    public bool HasPricingGaps { get; set; }
}
