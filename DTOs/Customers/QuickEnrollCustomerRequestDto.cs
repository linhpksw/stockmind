namespace stockmind.DTOs.Customers;

public class QuickEnrollCustomerRequestDto
{
    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }
}
