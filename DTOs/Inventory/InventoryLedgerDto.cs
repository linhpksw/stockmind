using stockmind.Models;

namespace stockmind.DTOs.Inventory
{
    public record LotDto(string LotId, decimal QtyOnHand, DateTime? ReceivedAt, DateOnly? ExpiryDate);

    public record MovementDto(
        string Id,
        string Type,
        decimal Qty,
        string? LotId,
        DateTime At,
        string? RefType,
        string? RefId
    );
    public class InventoryLedgerDto
    {
        private List<MovementDto> recentMovements;

        public InventoryLedgerDto(string ProductId, decimal OnHand, List<LotDto> Lots, List<MovementDto> RecentMovements)
        {
            this.ProductId = ProductId;
            this.OnHand = OnHand;
            this.Lots = Lots;
            recentMovements = RecentMovements;
        }

        public string ProductId { get; set; } = string.Empty;
        public decimal OnHand { get; set; }
        public IReadOnlyList<LotDto> Lots { get; set; } = [];
        public IReadOnlyList<MovementDto> Movements { get; set; } = [];
    }
}
