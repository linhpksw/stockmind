using System.Collections.Generic;

namespace stockmind.DTOs.SalesOrders;

public class CreateSalesOrderRequestDto
{
    public string? DraftOrderCode { get; set; }

    public long? CustomerId { get; set; }

    public string? CashierNotes { get; set; }

    public List<SalesOrderLineRequestDto> Lines { get; set; } = new();
}

public class SalesOrderLineRequestDto
{
    public long LotId { get; set; }

    public long ProductId { get; set; }

    public decimal Qty { get; set; }
}
