using System;

namespace stockmind.DTOs.Suppliers;

public class SupplierResponseDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Contact { get; set; }

    public int LeadTimeDays { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
