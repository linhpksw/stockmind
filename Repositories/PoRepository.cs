using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories {
    public class PoRepository {
        private readonly StockMindDbContext _context;

        public PoRepository(StockMindDbContext context) {
            _context = context;
        }

        public async Task AddAsync(Po po, CancellationToken cancellationToken) {
            await _context.Pos.AddAsync(po, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Po?> FindByIdAsync(long id, CancellationToken cancellationToken) {
            return await _context.Pos
                .Include(p => p.Supplier)
                .Include(p => p.Poitems)
                .FirstOrDefaultAsync(p => p.PoId == id, cancellationToken);
        }
    }
}
