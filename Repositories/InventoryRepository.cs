using System;
using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class InventoryRepository
    {

        private readonly StockMindDbContext _dbContext;
        public InventoryRepository(StockMindDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Inventory> AddAsync(Inventory inventory, CancellationToken cancellationToken)
        {
            await _dbContext.Inventories.AddAsync(inventory, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return inventory;
        }

        public Task<Inventory?> GetByIdAsync(long inventoryId, CancellationToken cancellationToken)
        {
            return _dbContext.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(s => s.InventoryId == inventoryId && !s.Deleted, cancellationToken);
        }

        public Task<Inventory?> GetByProductIdAsync(long productId, CancellationToken cancellationToken)
        {
            return _dbContext.Inventories
                .FirstOrDefaultAsync(s => s.ProductId == productId && !s.Deleted, cancellationToken);
        }

        public async Task<Inventory> UpdateAsync(Inventory inventory, CancellationToken cancellationToken)
        {
            _dbContext.Inventories.Update(inventory);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return inventory;
        }

        public async Task<PageResult<Inventory>> ListAsync(
            IQueryable<Inventory> baseQuery,
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

            return new PageResult<Inventory>(total, items);
        }

        public IQueryable<Inventory> Query()
        {
            return _dbContext.Inventories.AsQueryable();
        }

        public readonly record struct PageResult<T>(long Total, IReadOnlyCollection<T> Items);

        public async Task<List<Inventory>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Inventories
                .Where(i => !i.Deleted)
                .Include(i => i.Product)
                .ToListAsync(cancellationToken);
        }
        public async Task SoftDeleteAsync(long id, CancellationToken cancellationToken)
        {
            var inventory = await _dbContext.Inventories.FirstOrDefaultAsync(i => i.InventoryId == id, cancellationToken);
            if (inventory != null)
            {
                inventory.Deleted = true;
                inventory.LastModifiedAt = DateTime.UtcNow;
                _dbContext.Inventories.Update(inventory);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
