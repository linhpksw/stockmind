using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class SalesOrderItem
{
    public long OrderItemId { get; set; }

    public long OrderId { get; set; }

    public long ProductId { get; set; }

    public long? LotId { get; set; }

    public decimal Qty { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? AppliedMarkdownPercent { get; set; }

    public string? PriceOverrideReason { get; set; }

    public decimal LineSubtotal { get; set; }

    public decimal LineTotal { get; set; }

    public bool IsWeightBased { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual Lot? Lot { get; set; }

    public virtual SalesOrder Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
