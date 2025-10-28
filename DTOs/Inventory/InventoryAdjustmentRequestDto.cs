namespace stockmind.DTOs.Inventory
{
    public class InventoryAdjustmentRequestDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string LotId { get; set; } = string.Empty;
        public decimal QtyDelta { get; set; }
        public string Reason { get; set; } = null!;
        public long ActorId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
