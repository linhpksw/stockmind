namespace stockmind.DTOs.Customers;

public class CreateCustomerRequestDto
{
    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? LoyaltyCode { get; set; }
}
