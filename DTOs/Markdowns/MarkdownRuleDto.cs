using System;

namespace stockmind.DTOs.Markdowns
{
    public class MarkdownRuleDto
    {
        public long Id { get; set; }

        public string Scope { get; set; } = string.Empty;

        public string? CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public string? CategoryCode { get; set; }

        public int DaysToExpiry { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal FloorPercentOfCost { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastModifiedAt { get; set; }

        public bool Deleted { get; set; }
    }
}
