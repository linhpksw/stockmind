namespace stockmind.DTOs.Markdowns
{
    public class MarkdownApplyRequestDto
    {
        public string ProductId { get; set; } = string.Empty;

        public string LotId { get; set; } = string.Empty;

        public decimal DiscountPct { get; set; }

        public bool OverrideFloor { get; set; }
    }
}
