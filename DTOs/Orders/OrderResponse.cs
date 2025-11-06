namespace stockmind.DTOs.Orders
{
    public class OrderResponse
    {
        public string Id { get; set; } = null!;
        public List<MovementResponse> Movements { get; set; } = new();
        public List<AppliedLotResponse> AppliedLots { get; set; } = new();
    }

    public class MovementResponse
    {
        public string ProductId { get; set; } = null!;
        public string LotId { get; set; } = null!;
        public decimal Qty { get; set; }
        public string Type { get; set; } = null!;
    }

    public class AppliedLotResponse
    {
        public string LotId { get; set; } = null!;
        public decimal Qty { get; set; }
    }
}
