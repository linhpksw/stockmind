using System;
using System.Collections.Generic;

namespace stockmind.DTOs.Inventory;

public class InventorySummaryDto
{
    public long ProductId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? SupplierName { get; set; }
    public string Uom { get; set; } = "unit";
    public string? MediaUrl { get; set; }
    public decimal OnHand { get; set; }
    public IReadOnlyList<InventoryLotSummaryDto> Lots { get; set; } = Array.Empty<InventoryLotSummaryDto>();
}

public class InventoryLotSummaryDto
{
    public long LotId { get; set; }
    public string LotCode { get; set; } = string.Empty;
    public DateTime? ReceivedAt { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal QtyOnHand { get; set; }
    public decimal UnitCost { get; set; }
}
