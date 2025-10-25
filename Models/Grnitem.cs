using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Grnitem
{
    public long GrnItemId { get; set; }

    public long GrnId { get; set; }

    public long ProductId { get; set; }

    public long? LotId { get; set; }

    public decimal QtyReceived { get; set; }

    public decimal UnitCost { get; set; }

    public string? LotCode { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual Grn Grn { get; set; } = null!;

    public virtual Lot? Lot { get; set; }

    public virtual Product Product { get; set; } = null!;
}
