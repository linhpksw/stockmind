using System.ComponentModel.DataAnnotations;

namespace stockmind.DTOs.Suppliers;

public class UpdateSupplierRequestDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Contact { get; set; }

    [Range(0, int.MaxValue)]
    public int? LeadTimeDays { get; set; }
}
