using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Po
{
    public long PoId { get; set; }

    public long SupplierId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Grn> Grns { get; set; } = new List<Grn>();

    public virtual ICollection<Poitem> Poitems { get; set; } = new List<Poitem>();

    public virtual Supplier Supplier { get; set; } = null!;
}
