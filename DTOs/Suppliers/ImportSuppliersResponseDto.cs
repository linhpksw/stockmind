namespace stockmind.DTOs.Suppliers;

public class ImportSuppliersResponseDto
{
    public int Created { get; set; }

    public int Updated { get; set; }

    public int SkippedInvalid { get; set; }

    public int Total { get; set; }
}
