namespace stockmind.DTOs.Customers;

public class CustomerSummaryDto
{
    public long CustomerId { get; set; }

    public string? LoyaltyCode { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public int LoyaltyPoints { get; set; }
}
