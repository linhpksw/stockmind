using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class CategoryRepository
    {
        private readonly StockMindDbContext _dbContext;

        public CategoryRepository(StockMindDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Category?> GetByIdAsync(long categoryId, CancellationToken cancellationToken)
        {
            return _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId && !c.Deleted, cancellationToken);
        }

        public Task<Category?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        {
            var normalized = code.Trim();
            return _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == normalized && !c.Deleted, cancellationToken);
        }

        public Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken)
        {
            var normalized = name.Trim();
            return _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == normalized && !c.Deleted, cancellationToken);
        }

        public Task<List<Category>> ListAllAsync(bool includeDeleted, CancellationToken cancellationToken)
        {
            var query = _dbContext.Categories.AsNoTracking();
            if (!includeDeleted)
            {
                query = query.Where(c => !c.Deleted);
            }

            return query.ToListAsync(cancellationToken);
        }

        public Task<List<Category>> ListAllTrackedAsync(bool includeDeleted, CancellationToken cancellationToken)
        {
            var query = _dbContext.Categories.AsQueryable();
            if (!includeDeleted)
            {
                query = query.Where(c => !c.Deleted);
            }

            return query.ToListAsync(cancellationToken);
        }

        public Task AddRangeAsync(IEnumerable<Category> categories, CancellationToken cancellationToken)
        {
            return _dbContext.Categories.AddRangeAsync(categories, cancellationToken);
        }

        public async Task AddRangeWithExplicitIdsAsync(
            IEnumerable<Category> categories,
            CancellationToken cancellationToken)
        {
            var categoryList = categories.ToList();
            if (categoryList.Count == 0)
            {
                return;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Category ON;", cancellationToken);
            _dbContext.Categories.AddRange(categoryList);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Category OFF;", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
