using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories;

public class SupplierRepository
{
    private readonly StockMindDbContext _dbContext;

    public SupplierRepository(StockMindDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByNameAsync(string name, long? excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        return _dbContext.Suppliers
            .AsNoTracking()
            .Where(s => s.Name == normalized)
            .Where(s => !excludeId.HasValue || s.SupplierId != excludeId.Value)
            .AnyAsync(cancellationToken);
    }

    public async Task<Supplier> AddAsync(Supplier supplier, CancellationToken cancellationToken)
    {
        await _dbContext.Suppliers.AddAsync(supplier, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    public Task<Supplier?> GetByIdAsync(long supplierId, CancellationToken cancellationToken)
    {
        return _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierId == supplierId, cancellationToken);
    }

    public async Task<Supplier> UpdateAsync(Supplier supplier, CancellationToken cancellationToken)
    {
        _dbContext.Suppliers.Update(supplier);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    public async Task<PageResult<Supplier>> ListAsync(
        IQueryable<Supplier> baseQuery,
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

        return new PageResult<Supplier>(total, items);
    }

    public async Task<bool> ExistsByIdAsync(long supplierId, CancellationToken cancellationToken)
    {
        return await _dbContext.Suppliers.AnyAsync(s => s.SupplierId == supplierId && !s.Deleted, cancellationToken);
    }

    public IQueryable<Supplier> Query()
    {
        return _dbContext.Suppliers.AsQueryable();
    }

    public readonly record struct PageResult<T>(long Total, IReadOnlyCollection<T> Items);
}
