using stockmind.Models;

namespace stockmind.DTOs.Customers;

public static class CustomerMapper
{
    public static CustomerResponseDto ToDto(Customer customer)
    {
        return new CustomerResponseDto
        {
            Id = customer.CustomerId.ToString(),
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            LoyaltyCode = customer.LoyaltyCode,
            LoyaltyPoints = customer.LoyaltyPoints,
            CreatedAt = customer.CreatedAt
        };
    }
}
