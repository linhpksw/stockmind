using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class GrnRepository
    {
        private readonly StockMindDbContext _context;

        public GrnRepository(StockMindDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Grn grn, CancellationToken cancellationToken)
        {
            await _context.Grns.AddAsync(grn, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Grn?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            return await _context.Grns
                .Include(g => g.Grnitems)
                    .ThenInclude(item => item.Lot)
                        .ThenInclude(lot => lot!.StockMovements)
                .FirstOrDefaultAsync(g => g.GrnId == id && !g.Deleted, cancellationToken);
        }
    }
}

