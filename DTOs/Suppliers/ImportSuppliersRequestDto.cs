using System.Collections.Generic;

namespace stockmind.DTOs.Suppliers;

public class ImportSuppliersRequestDto
{
    public List<ImportSupplierRowDto> Rows { get; set; } = new();
}
