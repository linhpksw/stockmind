using System;
using System.Collections.Generic;

namespace stockmind.DTOs.Pos;

public class PurchaseOrderSummaryDto
{
    public long PoId { get; set; }
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalQty { get; set; }
    public decimal TotalCost { get; set; }
    public IReadOnlyList<PurchaseOrderItemSummaryDto> Items { get; set; } = Array.Empty<PurchaseOrderItemSummaryDto>();
}

public class PurchaseOrderItemSummaryDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal UnitCost { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public string? MediaUrl { get; set; }
}
