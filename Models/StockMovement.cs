using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class StockMovement
{
    public long MovementId { get; set; }

    public long ProductId { get; set; }

    public long? LotId { get; set; }

    public decimal Qty { get; set; }

    public string Type { get; set; } = null!;

    public string? RefType { get; set; }

    public long? RefId { get; set; }

    public long? ActorId { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual UserAccount? Actor { get; set; }

    public virtual Lot? Lot { get; set; }

    public virtual Product Product { get; set; } = null!;
}
