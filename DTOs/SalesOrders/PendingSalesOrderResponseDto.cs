using System;

namespace stockmind.DTOs.SalesOrders;

public class PendingSalesOrderResponseDto
{
    public long PendingId { get; set; }

    public Guid ConfirmationToken { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public string Message { get; set; } = string.Empty;
}
