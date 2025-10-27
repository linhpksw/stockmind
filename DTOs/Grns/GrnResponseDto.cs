namespace stockmind.DTOs.Grns {
    public class GrnResponseDto {
        public string Id { get; set; } = null!;
        public List<StockMovementDto> StockMovements { get; set; } = new();
    }

    public class StockMovementDto {
        public string ProductId { get; set; } = null!;
        public string LotId { get; set; } = null!;
        public decimal Qty { get; set; }
        public string Type { get; set; } = null!;
    }
}
