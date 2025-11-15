using System.Collections.Generic;

namespace stockmind.DTOs.Markdowns
{
    public class MarkdownApplyBulkRequestDto
    {
        public List<MarkdownApplyRequestDto> Items { get; set; } = new();
    }

    public class MarkdownApplyBulkResponseDto
    {
        public int Requested { get; set; }

        public int Applied { get; set; }

        public int Failed { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}
