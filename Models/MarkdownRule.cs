using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class MarkdownRule
{
    public long MarkdownRuleId { get; set; }

    public long? CategoryId { get; set; }

    public int DaysToExpiry { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal FloorPercentOfCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual Category? Category { get; set; }
}
