using System.Collections.Generic;

namespace stockmind.DTOs.Categories;

public class ImportCategoriesRequestDto
{
    public IReadOnlyList<ImportCategoryRowDto> Rows { get; set; } = new List<ImportCategoryRowDto>();
}
