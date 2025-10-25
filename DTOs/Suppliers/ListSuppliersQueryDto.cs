using Microsoft.AspNetCore.Mvc;

namespace stockmind.DTOs.Suppliers;

public class ListSuppliersQueryDto
{
    public int PageNum { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    [FromQuery(Name = "q")]
    public string? Query { get; set; }

    public string? Sort { get; set; }

    public bool IncludeDeleted { get; set; }

    public bool DeletedOnly { get; set; }
}
