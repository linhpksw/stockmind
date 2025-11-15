namespace stockmind.DTOs.Categories;

public class ImportCategoryRowDto
{
    public long? CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentCode { get; set; }
    public long? ParentCategoryId { get; set; }
}
