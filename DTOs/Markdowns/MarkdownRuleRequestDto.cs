namespace stockmind.DTOs.Markdowns
{
    public class MarkdownRuleRequestDto
    {
        public string? CategoryId { get; set; }

        public int DaysToExpiry { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal FloorPercentOfCost { get; set; }
    }
}
