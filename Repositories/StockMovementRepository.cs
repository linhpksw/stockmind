using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class StockMovementRepository
    {

        private readonly StockMindDbContext _dbContext;

        public StockMovementRepository(StockMindDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<StockMovement> AddAsync(StockMovement stockMovement, CancellationToken cancellationToken)
        {
            await _dbContext.StockMovements.AddAsync(stockMovement, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return stockMovement;
        }

        public Task<StockMovement?> GetByIdAsync(long stockMovementId, CancellationToken cancellationToken)
        {
            return _dbContext.StockMovements
                .FirstOrDefaultAsync(s => s.MovementId == stockMovementId, cancellationToken);
        }

        public async Task<StockMovement> UpdateAsync(StockMovement stockMovement, CancellationToken cancellationToken)
        {
            _dbContext.StockMovements.Update(stockMovement);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return stockMovement;
        }

        public async Task<PageResult<StockMovement>> ListAsync(
            IQueryable<StockMovement> baseQuery,
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

            return new PageResult<StockMovement>(total, items);
        }

        public IQueryable<StockMovement> Query()
        {
            return _dbContext.StockMovements.AsQueryable();
        }

        public readonly record struct PageResult<T>(long Total, IReadOnlyCollection<T> Items);

        public async Task<List<StockMovement>> GetRecentSalesAsync(DateTime fromDate, CancellationToken cancellationToken)
        {
            return await _dbContext.StockMovements
                .Where(sm => sm.Type == "OUT_SALE" && sm.CreatedAt >= fromDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<StockMovement>> GetSalesMovementsAsync(DateTime sinceDate, CancellationToken cancellationToken)
        {
            return await _dbContext.StockMovements
                .Include(sm => sm.Product)
                .Where(sm => sm.Type == "OUT_SALE" && sm.CreatedAt >= sinceDate)
                .OrderByDescending(sm => sm.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<StockMovement>> GetRecentMovementsAsync(long productId, int count, CancellationToken ct = default)
        {
            return await _dbContext.StockMovements
                .AsNoTracking()
                .Where(m => m.ProductId == productId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(count)
                .ToListAsync(ct);
        }
    }
}
