using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class MarginProfile
{
    public long MarginProfileId { get; set; }

    public long ParentCategoryId { get; set; }

    public string ParentCategoryName { get; set; } = null!;

    public string Profile { get; set; } = null!;

    public string PriceSensitivity { get; set; } = null!;

    public decimal MinMarginPct { get; set; }

    public decimal TargetMarginPct { get; set; }

    public decimal MaxMarginPct { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual Category ParentCategory { get; set; } = null!;
}
