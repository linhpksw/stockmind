namespace stockmind.DTOs.Margins
{
    public class ImportMarginProfileRowDto
    {
        public long? ParentCategoryId { get; set; }

        public string? ParentCategoryName { get; set; }

        public string MarginProfile { get; set; } = string.Empty;

        public string PriceSensitivity { get; set; } = string.Empty;

        public decimal? MinMarginPct { get; set; }

        public decimal? TargetMarginPct { get; set; }

        public decimal? MaxMarginPct { get; set; }

        public string? Notes { get; set; }
    }
}
