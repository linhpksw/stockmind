using System.Collections.Generic;

namespace stockmind.DTOs.Categories;

public class CategoryNodeDto
{
    public long CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long? ParentCategoryId { get; set; }
    public List<CategoryNodeDto> Children { get; set; } = new();
}
