using System.Net.Mail;
using System.Text.RegularExpressions;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.DTOs.Customers;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services;

public class CustomerService
{
    private readonly CustomerRepository _customerRepository;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(CustomerRepository customerRepository, ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<CustomerResponseDto?> LookupByPhoneAsync(string? phoneNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var customer = await _customerRepository.FindByPhoneAsync(phoneNumber, cancellationToken);
        return customer == null ? null : CustomerMapper.ToDto(customer);
    }

    public async Task<CustomerResponseDto> CreateAsync(CreateCustomerRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var fullName = NormalizeName(request.FullName);
        var phone = NormalizePhone(request.PhoneNumber);
        var email = NormalizeEmail(request.Email);

        ValidateName(fullName);
        ValidatePhone(phone);
        ValidateEmail(email);

        var exists = await _customerRepository.ExistsByPhoneAsync(phone, null, cancellationToken);
        if (exists)
        {
            throw new BizDataAlreadyExistsException(ErrorCode4xx.DataAlreadyExists, new[] { "phoneNumber" });
        }

        var customer = new Customer
        {
            FullName = fullName,
            PhoneNumber = phone,
            Email = email,
            LoyaltyCode = string.IsNullOrWhiteSpace(request.LoyaltyCode) ? GenerateLoyaltyCode(phone) : request.LoyaltyCode!.Trim(),
            LoyaltyPoints = 0,
            Notes = null,
            Deleted = false
        };

        var created = await _customerRepository.AddAsync(customer, cancellationToken);
        _logger.LogInformation("Created customer {CustomerId} with loyalty code {Code}", created.CustomerId, created.LoyaltyCode);
        return CustomerMapper.ToDto(created);
    }

    public async Task<Customer> GetByIdAsync(long customerId, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
                       ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"customerId={customerId}" });
        return customer;
    }

    public Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        return _customerRepository.UpdateAsync(customer, cancellationToken);
    }

    private static string NormalizeName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string NormalizePhone(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : Regex.Replace(value.Trim(), @"\s+", string.Empty);
    }

    private static string NormalizeEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static void ValidateName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "fullName" });
        }
    }

    private static void ValidatePhone(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "phoneNumber" });
        }

        if (phoneNumber.Length < 9 || phoneNumber.Length > 15)
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "phoneNumber" });
        }

        if (!Regex.IsMatch(phoneNumber, @"^\+?\d+$"))
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "phoneNumber" });
        }
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "email" });
        }

        try
        {
            _ = new MailAddress(email);
        }
        catch (FormatException)
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "email" });
        }
    }

    private static string GenerateLoyaltyCode(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        var suffix = digits.Length >= 4 ? digits[^4..] : digits.PadLeft(4, '0');
        return $"LC-{DateTime.UtcNow:yyyyMMddHHmm}-{suffix}";
    }
}
