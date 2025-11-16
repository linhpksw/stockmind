namespace stockmind.DTOs.SalesOrders;

public class CreateSalesOrderRequestDto
{
    public string? OrderCode { get; set; }

    public long? CustomerId { get; set; }

    public List<CreateSalesOrderLineDto> Lines { get; set; } = new();

    public int LoyaltyPointsToRedeem { get; set; }
}

public class CreateSalesOrderLineDto
{
    public long ProductId { get; set; }

    public long LotId { get; set; }

    public decimal Quantity { get; set; }
}
