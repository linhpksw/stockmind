using System.Collections.Generic;

namespace stockmind.DTOs.Margins
{
    public class ImportMarginProfilesRequestDto
    {
        public IReadOnlyList<ImportMarginProfileRowDto> Rows { get; set; } =
            new List<ImportMarginProfileRowDto>();
    }
}
