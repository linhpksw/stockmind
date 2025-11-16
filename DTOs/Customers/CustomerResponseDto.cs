namespace stockmind.DTOs.Customers;

public class CustomerResponseDto
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? LoyaltyCode { get; set; }

    public int LoyaltyPoints { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsUpdated { get; set; }
}
