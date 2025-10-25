using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Inventory
{
    public long InventoryId { get; set; }

    public long ProductId { get; set; }

    public decimal OnHand { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual Product Product { get; set; } = null!;
}
