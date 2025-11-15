using System.Linq;
using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class PoRepository
    {
        private readonly StockMindDbContext _context;

        public PoRepository(StockMindDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Po po, CancellationToken cancellationToken)
        {
            await _context.Pos.AddAsync(po, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Po?> FindByIdAsync(long id, CancellationToken cancellationToken)
        {
            return await _context.Pos
                .Include(p => p.Supplier)
                .Include(p => p.Poitems)
                .FirstOrDefaultAsync(p => p.PoId == id, cancellationToken);
        }

        public async Task<List<Poitem>> GetOpenOrderItemsAsync(CancellationToken cancellationToken)
        {
            return await _context.Poitems
                .Include(poi => poi.Product)
                .Include(poi => poi.Po)
                .Where(poi => poi.Po.Status == "OPEN")
                .ToListAsync(cancellationToken);
        }

        public async Task ClearOpenPosAsync(CancellationToken cancellationToken)
        {
            var openItems = await _context.Poitems
                .Where(item => item.Po.Status == "OPEN")
                .ToListAsync(cancellationToken);
            if (openItems.Count > 0)
            {
                _context.Poitems.RemoveRange(openItems);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var openPos = await _context.Pos
                .Where(po => po.Status == "OPEN")
                .ToListAsync(cancellationToken);
            if (openPos.Count > 0)
            {
                _context.Pos.RemoveRange(openPos);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<List<Po>> GetOpenPosWithItemsAsync(CancellationToken cancellationToken)
        {
            return await _context.Pos
                .Where(po => po.Status == "OPEN" && !po.Deleted)
                .Include(po => po.Supplier)
                .Include(po => po.Poitems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(p => p.Category)
                .Include(po => po.Poitems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(p => p.Supplier)
                .ToListAsync(cancellationToken);
        }

        public async Task<PageResult<Po>> ListPagedAsync(
            int pageNum,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = _context.Pos
                .Where(po => !po.Deleted)
                .Include(po => po.Supplier)
                .Include(po => po.Poitems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(p => p.Supplier)
                .Include(po => po.Poitems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(p => p.Category)
                .OrderByDescending(po => po.CreatedAt);

            var skip = (pageNum - 1) * pageSize;
            var total = await query.LongCountAsync(cancellationToken);
            var items = await query
                .Skip(skip)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return new PageResult<Po>(total, items);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        public readonly record struct PageResult<T>(long Total, IReadOnlyCollection<T> Items);
    }
}
