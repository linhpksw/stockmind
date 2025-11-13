using System.Collections.Generic;

namespace stockmind.DTOs.Product
{
    public class ImportProductsRequestDto
    {
        public IReadOnlyList<ImportProductRowDto> Rows { get; set; } = new List<ImportProductRowDto>();
    }
}
