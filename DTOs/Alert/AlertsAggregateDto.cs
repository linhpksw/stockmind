namespace stockmind.DTOs.Alert
{
    public record LowStockDto(string ProductId, decimal OnHand, int MinStock);
    public record ExpirySoonDto(string ProductId, string LotId, int DaysToExpiry);
    public record SlowMoverDto(string ProductId, int WindowDays, decimal UnitsSold);

    public class AlertsAggregateDto
    {
        public List<LowStockDto> LowStock { get; init; } = [];
        public List<ExpirySoonDto> ExpirySoon { get; init; } = [];
        public List<SlowMoverDto> SlowMovers { get; init; } = [];
    }
}
