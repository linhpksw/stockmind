using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class SalesOrderItemRepository
    {
        private readonly StockMindDbContext _dbContext;

        public SalesOrderItemRepository(StockMindDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<int> ApplyMarkdownAsync(long productId, decimal markdownPercent, CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;

            return _dbContext.SalesOrderItems
                .Where(item => item.ProductId == productId && !item.Deleted)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(i => i.AppliedMarkdownPercent, markdownPercent)
                        .SetProperty(i => i.LastModifiedAt, _ => utcNow),
                    cancellationToken);
        }
    }
}
