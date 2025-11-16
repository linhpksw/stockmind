using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class CustomerRepository
    {
        private readonly StockMindDbContext _dbContext;

        public CustomerRepository(StockMindDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Customer?> FindByIdAsync(long id, CancellationToken cancellationToken)
        {
            return _dbContext.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(customer => customer.CustomerId == id && !customer.Deleted, cancellationToken);
        }

        public Task<Customer?> FindByPhoneAsync(string phoneNumber, CancellationToken cancellationToken)
        {
            return _dbContext.Customers
                .FirstOrDefaultAsync(
                    customer => !customer.Deleted && customer.PhoneNumber == phoneNumber,
                    cancellationToken);
        }

        public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken)
        {
            await _dbContext.Customers.AddAsync(customer, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return customer;
        }

        public async Task<IReadOnlyList<Customer>> SearchByPhoneAsync(
            string normalizedPhone,
            int limit,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Customers
                .AsNoTracking()
                .Where(customer => !customer.Deleted && customer.PhoneNumber.Contains(normalizedPhone))
                .OrderBy(customer => customer.PhoneNumber)
                .ThenBy(customer => customer.FullName)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public Task<bool> LoyaltyCodeExistsAsync(string loyaltyCode, CancellationToken cancellationToken)
        {
            return _dbContext.Customers.AnyAsync(
                customer => customer.LoyaltyCode == loyaltyCode && !customer.Deleted,
                cancellationToken);
        }
    }
}
