using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class SalesOrderPending
{
    public long PendingId { get; set; }

    public long CashierId { get; set; }

    public long CustomerId { get; set; }

    public string PayloadJson { get; set; } = null!;

    public Guid ConfirmationToken { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ConfirmedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual UserAccount Cashier { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
