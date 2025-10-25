using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Product
{
    public long ProductId { get; set; }

    public string SkuCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public long? CategoryId { get; set; }

    public bool IsPerishable { get; set; }

    public int? ShelfLifeDays { get; set; }

    public string Uom { get; set; } = null!;

    public decimal Price { get; set; }

    public int MinStock { get; set; }

    public int LeadTimeDays { get; set; }

    public long? SupplierId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<Grnitem> Grnitems { get; set; } = new List<Grnitem>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<Lot> Lots { get; set; } = new List<Lot>();

    public virtual ICollection<Poitem> Poitems { get; set; } = new List<Poitem>();

    public virtual ICollection<ReplenishmentSuggestion> ReplenishmentSuggestions { get; set; } = new List<ReplenishmentSuggestion>();

    public virtual ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual Supplier? Supplier { get; set; }
}
