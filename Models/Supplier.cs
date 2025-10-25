using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Supplier
{
    public long SupplierId { get; set; }

    public string Name { get; set; } = null!;

    public string? Contact { get; set; }

    public int LeadTimeDays { get; set; }

    public virtual ICollection<Po> Pos { get; set; } = new List<Po>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
