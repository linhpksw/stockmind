using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories {
    public class StockMovementRepository {
        private readonly StockMindDbContext _context;

        public StockMovementRepository(StockMindDbContext context) {
            _context = context;
        }

        public async Task AddAsync(StockMovement movement, CancellationToken cancellationToken) {
            await _context.StockMovements.AddAsync(movement, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<StockMovement>> GetRecentSalesAsync(DateTime fromDate, CancellationToken cancellationToken) {
            return await _context.StockMovements
                .Where(sm => sm.Type == "OUT_SALE" && sm.CreatedAt >= fromDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<StockMovement>> GetSalesMovementsAsync(DateTime sinceDate, CancellationToken cancellationToken) {
            return await _context.StockMovements
                .Include(sm => sm.Product)
                .Where(sm => sm.Type == "OUT_SALE" && sm.CreatedAt >= sinceDate)
                .OrderByDescending(sm => sm.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
