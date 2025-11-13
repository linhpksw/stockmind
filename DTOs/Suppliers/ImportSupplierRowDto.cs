namespace stockmind.DTOs.Suppliers;

public class ImportSupplierRowDto
{
    public string? SupplierId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Contact { get; set; }

    public int? LeadTimeDays { get; set; }
}
