using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace stockmind.DTOs.SalesOrders;

public class SalesOrderCatalogQueryDto
{
    private const int DefaultPageSize = 12;
    private const int MaxPageSize = 40;

    public int PageNum { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;

    public string? Search { get; set; }

    public string? ParentCategoryIds { get; set; }

    public string? CategoryIds { get; set; }

    public string? SupplierIds { get; set; }

    [JsonIgnore]
    public IReadOnlyCollection<long> ParentCategoryIdList { get; private set; } = Array.Empty<long>();

    [JsonIgnore]
    public IReadOnlyCollection<long> CategoryIdList { get; private set; } = Array.Empty<long>();

    [JsonIgnore]
    public IReadOnlyCollection<long> SupplierIdList { get; private set; } = Array.Empty<long>();

    public void Normalize()
    {
        if (PageNum <= 0)
        {
            PageNum = 1;
        }

        if (PageSize <= 0)
        {
            PageSize = DefaultPageSize;
        }

        if (PageSize > MaxPageSize)
        {
            PageSize = MaxPageSize;
        }

        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        ParentCategoryIdList = ParseIds(ParentCategoryIds);
        CategoryIdList = ParseIds(CategoryIds);
        SupplierIdList = ParseIds(SupplierIds);
    }

    private static IReadOnlyCollection<long> ParseIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<long>();
        }

        var tokens = raw
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => long.TryParse(token, out var id) ? id : (long?)null)
            .Where(id => id.HasValue && id.Value > 0)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        return tokens;
    }

}
