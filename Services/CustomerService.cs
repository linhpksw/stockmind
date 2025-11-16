using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.DTOs.Customers;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class CustomerService
    {
        private readonly CustomerRepository _customerRepository;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(CustomerRepository customerRepository, ILogger<CustomerService> logger)
        {
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<CustomerSummaryDto>> SearchByPhoneAsync(
            CustomerSearchQueryDto query,
            CancellationToken cancellationToken)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            query.Normalize();
            if (string.IsNullOrWhiteSpace(query.Phone))
            {
                return Array.Empty<CustomerSummaryDto>();
            }

            var normalizedPhone = NormalizePhone(query.Phone);
            if (normalizedPhone.Length < 3)
            {
                return Array.Empty<CustomerSummaryDto>();
            }

            var matches = await _customerRepository.SearchByPhoneAsync(normalizedPhone, query.Limit, cancellationToken);
            return matches.Select(MapToDto).ToList();
        }

        public async Task<CustomerSummaryDto> QuickEnrollAsync(
            QuickEnrollCustomerRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "fullName" });
            }

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "phoneNumber" });
            }

            var normalizedName = request.FullName.Trim();
            var normalizedPhone = NormalizePhone(request.PhoneNumber);

            if (normalizedPhone.Length < 9)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "phoneNumber" });
            }

            if (!Regex.IsMatch(normalizedPhone, "^\\+?[0-9]{9,15}$"))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "phoneNumber" });
            }

            var normalizedEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedEmail) && !IsValidEmail(normalizedEmail))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "email" });
            }

            var existing = await _customerRepository.FindByPhoneAsync(normalizedPhone, cancellationToken);
            if (existing != null)
            {
                throw new BizDataAlreadyExistsException(ErrorCode4xx.DataAlreadyExists, new[] { normalizedPhone });
            }

            var loyaltyCode = await GenerateUniqueLoyaltyCodeAsync(cancellationToken);
            var utcNow = DateTime.UtcNow;
            var customer = new Customer
            {
                FullName = normalizedName,
                PhoneNumber = normalizedPhone,
                Email = normalizedEmail,
                LoyaltyCode = loyaltyCode,
                LoyaltyPoints = 0,
                CreatedAt = utcNow,
                LastModifiedAt = utcNow,
                Deleted = false
            };

            await _customerRepository.AddAsync(customer, cancellationToken);
            _logger.LogInformation("Customer quick enrolled with phone {Phone}", normalizedPhone);
            return MapToDto(customer);
        }

        private static bool IsValidEmail(string value)
        {
            try
            {
                _ = new MailAddress(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<Customer?> FindByIdAsync(long customerId, CancellationToken cancellationToken)
        {
            return _customerRepository.FindByIdAsync(customerId, cancellationToken);
        }

        private async Task<string> GenerateUniqueLoyaltyCodeAsync(CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var candidate = BuildLoyaltyCode();
                var exists = await _customerRepository.LoyaltyCodeExistsAsync(candidate, cancellationToken);
                if (!exists)
                {
                    return candidate;
                }
            }

            _logger.LogWarning("Failed to generate a unique loyalty code after multiple attempts.");
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "loyaltyCode" });
        }

        private static string BuildLoyaltyCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var buffer = new byte[4];
            rng.GetBytes(buffer);
            var suffix = BitConverter.ToUInt32(buffer, 0) % 100000;
            return $"LC-{DateTime.UtcNow:yyyyMMdd}-{suffix:D5}";
        }

        private static string NormalizePhone(string raw)
        {
            var builder = new StringBuilder(raw.Length);
            foreach (var ch in raw.Trim())
            {
                if (char.IsDigit(ch) || ch == '+')
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString();
        }

        private static CustomerSummaryDto MapToDto(Customer customer)
        {
            return new CustomerSummaryDto
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                PhoneNumber = customer.PhoneNumber,
                LoyaltyCode = customer.LoyaltyCode,
                LoyaltyPoints = customer.LoyaltyPoints,
                Email = customer.Email
            };
        }
    }
}
