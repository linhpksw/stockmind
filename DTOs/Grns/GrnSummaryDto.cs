using System;
using System.Collections.Generic;

namespace stockmind.DTOs.Grns;

public class GrnSummaryDto
{
    public long GrnId { get; set; }
    public long PoId { get; set; }
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string Status { get; set; } = "RECEIVED";
    public decimal TotalQty { get; set; }
    public decimal TotalCost { get; set; }
    public IReadOnlyList<GrnItemSummaryDto> Items { get; set; } = Array.Empty<GrnItemSummaryDto>();
}

public class GrnItemSummaryDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QtyReceived { get; set; }
    public decimal UnitCost { get; set; }
    public string? LotCode { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public string? MediaUrl { get; set; }
}
