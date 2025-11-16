using Microsoft.AspNetCore.Mvc;

namespace stockmind.DTOs.SalesOrders;

public class SellableLotQueryDto
{
    [FromQuery(Name = "query")]
    public string? SearchTerm { get; set; }

    public List<long>? ParentCategoryIds { get; set; }

    public List<long>? CategoryIds { get; set; }

    public List<long>? SupplierIds { get; set; }

    public int Limit { get; set; } = 25;
}
