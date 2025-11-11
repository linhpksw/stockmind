using System;

namespace stockmind.Models;

public class ProductAuditLog
{
    public long ProductAuditLogId { get; set; }

    public long ProductId { get; set; }

    public string Action { get; set; } = null!;

    public string Actor { get; set; } = null!;

    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
