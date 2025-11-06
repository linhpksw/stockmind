namespace stockmind.DTOs.Orders
{
    public class OrderRequest
    {
        public string Source { get; set; } = null!;
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public string ProductId { get; set; } = null!;
        public decimal Qty { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
