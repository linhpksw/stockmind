using System.Collections.Generic;
using System.Linq;
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

        public async Task<PageResult<Grn>> ListPagedWithItemsAsync(
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var normalizedSkip = Math.Max(0, skip);
            var normalizedTake = Math.Max(0, take);

            var query = _context.Grns
                .Where(g => !g.Deleted)
                .Include(g => g.Grnitems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(p => p!.Supplier)
                .Include(g => g.Grnitems)
                    .ThenInclude(item => item.Lot)
                .OrderByDescending(g => g.ReceivedAt);

            var total = await query.LongCountAsync(cancellationToken);

            List<Grn> items;
            if (normalizedTake == 0)
            {
                items = new List<Grn>();
            }
            else
            {
                items = await query
                    .Skip(normalizedSkip)
                    .Take(normalizedTake)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
            }

            return new PageResult<Grn>(total, items);
        }

        public readonly record struct PageResult<T>(long Total, IReadOnlyCollection<T> Items);
    }
}

