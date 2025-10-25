using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Poitem
{
    public long PoItemId { get; set; }

    public long PoId { get; set; }

    public long ProductId { get; set; }

    public decimal QtyOrdered { get; set; }

    public DateOnly? ExpectedDate { get; set; }

    public decimal UnitCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual Po Po { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
