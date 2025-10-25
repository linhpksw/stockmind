using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Inventory
{
    public long InventoryId { get; set; }

    public long ProductId { get; set; }

    public decimal OnHand { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
