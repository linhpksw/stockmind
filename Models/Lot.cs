using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Lot
{
    public long LotId { get; set; }

    public long ProductId { get; set; }

    public string LotCode { get; set; } = null!;

    public DateTime ReceivedAt { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public decimal QtyOnHand { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<Grnitem> Grnitems { get; set; } = new List<Grnitem>();

    public virtual ICollection<LotSaleDecision> LotSaleDecisions { get; set; } = new List<LotSaleDecision>();

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
