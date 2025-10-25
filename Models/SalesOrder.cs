using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class SalesOrder
{
    public long OrderId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();
}
