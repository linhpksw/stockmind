using stockmind.DTOs.Alert;

namespace stockmind.Utils
{
    public static class AlertsMapper
    {
        public static LowStockDto ToDto((string ProductId, decimal OnHand, int MinStock) r)
            => new LowStockDto(r.ProductId, r.OnHand, r.MinStock);

        public static ExpirySoonDto ToDto((string ProductId, string LotId, DateOnly ExpiryDate, decimal QtyOnHand) r)
        {
            var days = (int)Math.Ceiling((r.ExpiryDate.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).TotalDays);
            return new ExpirySoonDto(r.ProductId, r.LotId, Math.Max(0, days));
        }

        public static SlowMoverDto ToDto((string ProductId, decimal UnitsSold) r, int windowDays)
            => new SlowMoverDto(r.ProductId, windowDays, r.UnitsSold);
    }
}
