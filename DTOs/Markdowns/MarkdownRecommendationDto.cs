using System;

namespace stockmind.DTOs.Markdowns
{
    public class MarkdownRecommendationDto
    {
        public string ProductId { get; set; } = string.Empty;

        public string LotId { get; set; } = string.Empty;

        public long LotEntityId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public long? CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public decimal UnitCost { get; set; }

        public decimal ListPrice { get; set; }

        public decimal QtyReceived { get; set; }

        public long? LotSaleDecisionId { get; set; }

        public bool LotSaleDecisionApplied { get; set; }

        public int DaysToExpiry { get; set; }

        public DateTime ReceivedAt { get; set; }

        public decimal SuggestedDiscountPct { get; set; }

        public decimal FloorPctOfCost { get; set; }

        public decimal? FloorSafeDiscountPct { get; set; }

        public bool RequiresFloorOverride { get; set; }
    }
}
