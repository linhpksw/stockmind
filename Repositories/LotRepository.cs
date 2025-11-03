using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class LotRepository
    {
        private readonly StockMindDbContext _dbContext;

        public LotRepository(StockMindDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Lot> AddAsync(Lot lot, CancellationToken cancellationToken)
        {
            await _dbContext.Lots.AddAsync(lot, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return lot;
        }

        public Task<Lot?> GetByIdAsync(long lotId, CancellationToken cancellationToken)
        {
            return _dbContext.Lots
                .FirstOrDefaultAsync(s => s.LotId == lotId, cancellationToken);
        }

        public Task<Lot?> GetForProductAsync(long lotId, long productId, CancellationToken cancellationToken)
        {
            return _dbContext.Lots
                .FirstOrDefaultAsync(s => s.LotId == lotId && s.ProductId == productId, cancellationToken);
        }

        public async Task<Lot> UpdateAsync(Lot lot, CancellationToken cancellationToken)
        {
            _dbContext.Lots.Update(lot);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return lot;
        }

        public async Task<PageResult<Lot>> ListAsync(
            IQueryable<Lot> baseQuery,
            int pageNum,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var skip = (pageNum - 1) * pageSize;
            var total = await baseQuery.LongCountAsync(cancellationToken);
            var items = await baseQuery
                .Skip(skip)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return new PageResult<Lot>(total, items);
        }

        public IQueryable<Lot> Query()
        {
            return _dbContext.Lots.AsQueryable();
        }

        public readonly record struct PageResult<T>(long Total, IReadOnlyCollection<T> Items);

        public async Task<Lot?> FindByProductIdAndLotCodeAsync(long productId, string lotCode, CancellationToken cancellationToken)
        {
            return await _dbContext.Lots
                .FirstOrDefaultAsync(
                    l => l.ProductId == productId &&
                         l.LotCode == lotCode &&
                         !l.Deleted,
                    cancellationToken
                );
        }
    }
}
