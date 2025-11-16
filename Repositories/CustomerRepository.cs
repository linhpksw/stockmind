using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories;

public class CustomerRepository
{
    private readonly StockMindDbContext _dbContext;

    public CustomerRepository(StockMindDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Customer?> FindByPhoneAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var normalized = Normalize(phoneNumber);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.FromResult<Customer?>(null);
        }

        return _dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                customer => !customer.Deleted && customer.PhoneNumber == normalized,
                cancellationToken);
    }

    public Task<Customer?> GetByIdAsync(long customerId, CancellationToken cancellationToken)
    {
        return _dbContext.Customers
            .FirstOrDefaultAsync(customer => customer.CustomerId == customerId && !customer.Deleted, cancellationToken);
    }

    public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        customer.CreatedAt = DateTime.UtcNow;
        customer.LastModifiedAt = customer.CreatedAt;
        customer.Deleted = false;
        await _dbContext.Customers.AddAsync(customer, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        customer.LastModifiedAt = DateTime.UtcNow;
        _dbContext.Customers.Update(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public Task<bool> ExistsByPhoneAsync(string phoneNumber, long? excludeCustomerId, CancellationToken cancellationToken)
    {
        var normalized = Normalize(phoneNumber);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.FromResult(false);
        }

        return _dbContext.Customers
            .AsNoTracking()
            .Where(customer => !customer.Deleted)
            .Where(customer => customer.PhoneNumber == normalized)
            .Where(customer => !excludeCustomerId.HasValue || customer.CustomerId != excludeCustomerId.Value)
            .AnyAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Normalize(string value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
