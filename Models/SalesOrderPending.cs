using System;

namespace stockmind.Models;

public class SalesOrderPending
{
    public long PendingId { get; set; }

    public long CashierId { get; set; }

    public long CustomerId { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public Guid ConfirmationToken { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual UserAccount Cashier { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
