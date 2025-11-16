using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class SalesOrder
{
    public long OrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public long CashierId { get; set; }

    public long? CustomerId { get; set; }

    public string? CashierNotes { get; set; }

    public int ItemsCount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal Total { get; set; }

    public int LoyaltyPointsEarned { get; set; }

    public int LoyaltyPointsRedeemed { get; set; }

    public decimal LoyaltyAmountRedeemed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual UserAccount Cashier { get; set; } = null!;

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();
}
