namespace stockmind.DTOs.Markdowns
{
    public class MarkdownRecommendationDto
    {
        public string ProductId { get; set; } = string.Empty;

        public string LotId { get; set; } = string.Empty;

        public int DaysToExpiry { get; set; }

        public decimal SuggestedDiscountPct { get; set; }

        public decimal FloorPctOfCost { get; set; }
    }
}
