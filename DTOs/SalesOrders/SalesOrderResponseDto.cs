using System;
using System.Collections.Generic;

namespace stockmind.DTOs.SalesOrders;

public class SalesOrderResponseDto
{
    public long OrderId { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int ItemsCount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal Total { get; set; }

    public long CashierId { get; set; }

    public long? CustomerId { get; set; }

    public IReadOnlyList<SalesOrderLineResponseDto> Lines { get; set; } = Array.Empty<SalesOrderLineResponseDto>();
}

public class SalesOrderLineResponseDto
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SkuCode { get; set; } = string.Empty;

    public long LotId { get; set; }

    public string LotCode { get; set; } = string.Empty;

    public decimal Qty { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public decimal LineSubtotal { get; set; }

    public decimal LineTotal { get; set; }

    public string Uom { get; set; } = string.Empty;
}

public class SalesOrderSeedDto
{
    public string OrderCode { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }
}
