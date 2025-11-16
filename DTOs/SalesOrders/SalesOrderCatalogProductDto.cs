using System;
using System.Collections.Generic;

namespace stockmind.DTOs.SalesOrders;

public class SalesOrderCatalogProductDto
{
    public long ProductId { get; set; }

    public string SkuCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Uom { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsPerishable { get; set; }

    public string? MediaUrl { get; set; }

    public string? SupplierName { get; set; }

    public long? SupplierId { get; set; }

    public string? CategoryName { get; set; }

    public long? CategoryId { get; set; }

    public string? ParentCategoryName { get; set; }

    public long? ParentCategoryId { get; set; }

    public decimal OnHand { get; set; }

    public int MinStock { get; set; }

    public decimal? SuggestedQty { get; set; }

    public decimal? MarginFloorPercent { get; set; }

    public decimal? TargetMarginPercent { get; set; }

    public IReadOnlyList<SalesOrderCatalogLotDto> Lots { get; set; } = Array.Empty<SalesOrderCatalogLotDto>();
}

public class SalesOrderCatalogLotDto
{
    public long LotId { get; set; }

    public string LotCode { get; set; } = string.Empty;

    public DateTime? ReceivedAt { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public decimal QtyOnHand { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal? UnitCost { get; set; }

    public decimal ComputedPrice { get; set; }
}
