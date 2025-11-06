namespace stockmind.DTOs.ReplenishmentSuggestion
{
    public class ReplenishmentSuggestionDto
    {
        public long ProductId { get; set; }
        public decimal OnHand { get; set; }
        public decimal OnOrder { get; set; }
        public double AvgDaily { get; set; }
        public double SigmaDaily { get; set; }
        public int LeadTimeDays { get; set; }
        public double SafetyStock { get; set; }
        public double ROP { get; set; }
        public double SuggestedQty { get; set; }
    }
}
