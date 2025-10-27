using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories {
    public class ProductRepository {
        private readonly StockMindDbContext _context;

        public ProductRepository(StockMindDbContext context) {
            _context = context;
        }

        #region Exists

        public async Task<bool> ExistsByIdAsync(long productId, CancellationToken cancellationToken) {
            return await _context.Products.AnyAsync(p => p.ProductId == productId && !p.Deleted, cancellationToken);
        }

        #endregion

        #region CRUD

        public async Task<List<Product>> GetAllAsync(CancellationToken cancellationToken) {
            return await _context.Products
                .Where(p => !p.Deleted)
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .ToListAsync(cancellationToken);
        }

        public async Task<Product?> FindByIdAsync(long id, CancellationToken cancellationToken) {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.ProductId == id && !p.Deleted, cancellationToken);
        }

        public async Task AddAsync(Product product, CancellationToken cancellationToken) {
            product.CreatedAt = DateTime.UtcNow;
            product.LastModifiedAt = DateTime.UtcNow;
            await _context.Products.AddAsync(product, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Product product, CancellationToken cancellationToken) {
            product.LastModifiedAt = DateTime.UtcNow;
            _context.Products.Update(product);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken) {
            var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
            if (product != null) {
                product.Deleted = true;
                product.LastModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        #endregion
    }
}
