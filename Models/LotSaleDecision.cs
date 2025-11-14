using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class LotSaleDecision
{
    public long LotSaleDecisionId { get; set; }

    public long LotId { get; set; }

    public decimal DiscountPercent { get; set; }

    public bool IsApplied { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual Lot Lot { get; set; } = null!;
}
