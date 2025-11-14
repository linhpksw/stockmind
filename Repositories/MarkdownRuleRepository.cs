using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public async Task<List<MarkdownRule>> ListAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.MarkdownRules
                .AsNoTracking()
                .Include(rule => rule.Category)
                .Where(rule => !rule.Deleted)
                .ToListAsync(cancellationToken);
        }

        public Task<MarkdownRule?> GetByIdAsync(long ruleId, CancellationToken cancellationToken)
        {
            return _dbContext.MarkdownRules
                .FirstOrDefaultAsync(rule => rule.MarkdownRuleId == ruleId && !rule.Deleted, cancellationToken);
        }

        public Task<MarkdownRule?> GetDetailedByIdAsync(long ruleId, CancellationToken cancellationToken)
        {
            return _dbContext.MarkdownRules
                .AsNoTracking()
                .Include(rule => rule.Category)
                .FirstOrDefaultAsync(rule => rule.MarkdownRuleId == ruleId && !rule.Deleted, cancellationToken);
        }

        public async Task<MarkdownRule> AddAsync(MarkdownRule rule, CancellationToken cancellationToken)
        {
            await _dbContext.MarkdownRules.AddAsync(rule, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return rule;
        }

        public async Task<MarkdownRule> UpdateAsync(MarkdownRule rule, CancellationToken cancellationToken)
        {
            _dbContext.MarkdownRules.Update(rule);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return rule;
        }

        public async Task SoftDeleteAsync(MarkdownRule rule, CancellationToken cancellationToken)
        {
            _dbContext.MarkdownRules.Update(rule);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsForScopeAsync(long? categoryId, int daysToExpiry, long? excludeRuleId, CancellationToken cancellationToken)
        {
            var query = _dbContext.MarkdownRules
                .AsNoTracking()
                .Where(rule => !rule.Deleted)
                .Where(rule => rule.DaysToExpiry == daysToExpiry);

            if (categoryId.HasValue)
            {
                query = query.Where(rule => rule.CategoryId == categoryId.Value);
            }
            else
            {
                query = query.Where(rule => rule.CategoryId == null);
            }

            if (excludeRuleId.HasValue)
            {
                query = query.Where(rule => rule.MarkdownRuleId != excludeRuleId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }
    }
}
