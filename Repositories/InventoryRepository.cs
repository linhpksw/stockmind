using System;
using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories {
    public class InventoryRepository {
        private readonly StockMindDbContext _context;

        public InventoryRepository(StockMindDbContext context) {
            _context = context;
        }

        public async Task<Inventory?> GetByProductIdAsync(long productId, CancellationToken cancellationToken) {
            return await _context.Inventories
                .Where(i => i.ProductId == productId && !i.Deleted)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(Inventory inventory, CancellationToken cancellationToken) {
            await _context.Inventories.AddAsync(inventory, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Inventory inventory, CancellationToken cancellationToken) {
            _context.Inventories.Update(inventory);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Inventory>> GetAllAsync(CancellationToken cancellationToken) {
            return await _context.Inventories
                .Where(i => !i.Deleted)
                .Include(i => i.Product)
                .ToListAsync(cancellationToken);
        }

        public async Task<Inventory?> GetByIdAsync(long id, CancellationToken cancellationToken) {
            return await _context.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.InventoryId == id && !i.Deleted, cancellationToken);
        }

        public async Task SoftDeleteAsync(long id, CancellationToken cancellationToken) {
            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.InventoryId == id, cancellationToken);
            if (inventory != null) {
                inventory.Deleted = true;
                inventory.LastModifiedAt = DateTime.UtcNow;
                _context.Inventories.Update(inventory);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
