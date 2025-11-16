using System;

namespace stockmind.DTOs.Customers;

public class CustomerSearchQueryDto
{
    private const int DefaultLimit = 5;
    private const int MaxLimit = 25;

    public string? Phone { get; set; }

    public int Limit { get; set; } = DefaultLimit;

    public void Normalize()
    {
        if (Limit <= 0)
        {
            Limit = DefaultLimit;
        }

        if (Limit > MaxLimit)
        {
            Limit = MaxLimit;
        }

        Phone = Phone?.Trim();
    }
}
