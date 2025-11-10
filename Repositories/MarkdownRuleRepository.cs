using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class MarkdownRuleRepository
    {
        private readonly StockMindDbContext _dbContext;

        public MarkdownRuleRepository(StockMindDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<MarkdownRule>> GetActiveRulesAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.MarkdownRules
                .AsNoTracking()
                .Where(rule => !rule.Deleted)
                .ToListAsync(cancellationToken);
        }
    }
}
