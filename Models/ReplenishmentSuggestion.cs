using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class ReplenishmentSuggestion
{
    public long ReplId { get; set; }

    public long ProductId { get; set; }

    public decimal OnHand { get; set; }

    public decimal OnOrder { get; set; }

    public decimal? AvgDaily { get; set; }

    public decimal? SigmaDaily { get; set; }

    public int LeadTimeDays { get; set; }

    public decimal SafetyStock { get; set; }

    public decimal Rop { get; set; }

    public decimal SuggestedQty { get; set; }

    public DateTime ComputedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual Product Product { get; set; } = null!;
}
