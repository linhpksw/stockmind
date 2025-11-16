using System;

namespace stockmind.DTOs.SalesOrders;

public class PendingSalesOrderStatusDto
{
    public long PendingId { get; set; }

    public string Status { get; set; } = "PENDING";

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public bool IsConfirmed => string.Equals(Status, "CONFIRMED", StringComparison.OrdinalIgnoreCase);
}
