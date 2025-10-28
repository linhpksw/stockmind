using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories
{
    public class ProductRepository
    {
        private readonly StockMindDbContext _dbContext;

        public ProductRepository(StockMindDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<bool> ExistsByNameAsync(string name, long? excludeId, CancellationToken cancellationToken)
        {
            var normalized = name.Trim();
            return _dbContext.Products
                .AsNoTracking()
                .Where(s => s.Name == normalized)
                .Where(s => !excludeId.HasValue || s.SupplierId != excludeId.Value)
                .AnyAsync(cancellationToken);
        }

        public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken)
        {
            await _dbContext.Products.AddAsync(product, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return product;
        }

        public Task<Product?> GetByIdAsync(long productId, CancellationToken cancellationToken)
        {
            return _dbContext.Products
                .FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);
        }

        public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken)
        {
            _dbContext.Products.Update(product);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return product;
        }

        public async Task<PageResult<Product>> ListAsync(
            IQueryable<Product> baseQuery,
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

            return new PageResult<Product>(total, items);
        }

        public IQueryable<Product> Query()
        {
            return _dbContext.Products.AsQueryable();
        }

        public readonly record struct PageResult<T>(long Total, IReadOnlyCollection<T> Items);

        #region Exists

        public async Task<bool> ExistsByIdAsync(long productId, CancellationToken cancellationToken)
        {
            return await _context.Products.AnyAsync(p => p.ProductId == productId && !p.Deleted, cancellationToken);
        }

        #endregion

        #region CRUD

        public async Task<List<Product>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Products
                .Where(p => !p.Deleted)
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .ToListAsync(cancellationToken);
        }

        public async Task<Product?> FindByIdAsync(long id, CancellationToken cancellationToken)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.ProductId == id && !p.Deleted, cancellationToken);
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken)
        {
            var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
            if (product != null)
            {
                product.Deleted = true;
                product.LastModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        #endregion
    }
}
