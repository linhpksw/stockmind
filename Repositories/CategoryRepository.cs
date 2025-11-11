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
    }
}
