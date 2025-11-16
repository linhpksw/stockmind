using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Customer
{
    public long CustomerId { get; set; }

    public string? LoyaltyCode { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Email { get; set; }

    public int LoyaltyPoints { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<SalesOrderPending> SalesOrderPendings { get; set; } = new List<SalesOrderPending>();

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
