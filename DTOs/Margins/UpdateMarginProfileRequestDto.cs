namespace stockmind.DTOs.Margins
{
    public class UpdateMarginProfileRequestDto
    {
        public decimal MinMarginPct { get; set; }

        public decimal TargetMarginPct { get; set; }

        public decimal MaxMarginPct { get; set; }
    }
}
