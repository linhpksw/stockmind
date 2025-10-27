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
    }
}
