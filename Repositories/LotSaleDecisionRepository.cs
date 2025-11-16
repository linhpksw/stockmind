using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class LotSaleDecisionRepository
    {
        private readonly StockMindDbContext _dbContext;

        public LotSaleDecisionRepository(StockMindDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<LotSaleDecision> AddAsync(long lotId, decimal discountPercent, bool isApplied, CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;
            var decision = new LotSaleDecision
            {
                LotId = lotId,
                DiscountPercent = discountPercent,
                IsApplied = isApplied,
                CreatedAt = utcNow,
                LastModifiedAt = utcNow,
                Deleted = false
            };

            await _dbContext.LotSaleDecisions.AddAsync(decision, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return decision;
        }

        public async Task<Dictionary<long, LotSaleDecision>> GetLatestByLotIdsAsync(
            IEnumerable<long> lotIds,
            CancellationToken cancellationToken)
        {
            var ids = lotIds?.Distinct().ToList() ?? new List<long>();
            if (ids.Count == 0)
            {
                return new Dictionary<long, LotSaleDecision>();
            }

            var decisions = await _dbContext.LotSaleDecisions
                .AsNoTracking()
                .Where(decision => !decision.Deleted && ids.Contains(decision.LotId))
                .OrderByDescending(decision => decision.LastModifiedAt)
                .ToListAsync(cancellationToken);

            return decisions
                .GroupBy(decision => decision.LotId)
                .ToDictionary(group => group.Key, group => group.First());
        }

        public async Task<bool> UpdateIsAppliedAsync(long decisionId, bool isApplied, CancellationToken cancellationToken)
        {
            var decision = await _dbContext.LotSaleDecisions
                .FirstOrDefaultAsync(item => item.LotSaleDecisionId == decisionId && !item.Deleted, cancellationToken);

            if (decision == null)
            {
                return false;
            }

            decision.IsApplied = isApplied;
            decision.LastModifiedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
