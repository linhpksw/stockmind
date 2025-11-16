using Microsoft.EntityFrameworkCore;
using stockmind.Models;

namespace stockmind.Repositories;

public class SalesOrderRepository
{
    private readonly StockMindDbContext _dbContext;

    public SalesOrderRepository(StockMindDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SalesOrder> AddAsync(SalesOrder order, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        order.CreatedAt = utcNow;
        order.LastModifiedAt = utcNow;

        await _dbContext.SalesOrders.AddAsync(order, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<SalesOrderPending> AddPendingAsync(SalesOrderPending pending, CancellationToken cancellationToken)
    {
        pending.CreatedAt = DateTime.UtcNow;
        await _dbContext.SalesOrderPendings.AddAsync(pending, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return pending;
    }

    public Task<SalesOrderPending?> GetPendingByTokenAsync(Guid token, CancellationToken cancellationToken)
    {
        return _dbContext.SalesOrderPendings
            .FirstOrDefaultAsync(
                pending => pending.ConfirmationToken == token,
                cancellationToken);
    }

    public async Task<SalesOrderPending> UpdatePendingAsync(SalesOrderPending pending, CancellationToken cancellationToken)
    {
        _dbContext.SalesOrderPendings.Update(pending);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return pending;
    }

    public Task<bool> ExistsByCodeAsync(string orderCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return Task.FromResult(false);
        }

        var normalized = orderCode.Trim();
        return _dbContext.SalesOrders.AnyAsync(order => order.OrderCode == normalized && !order.Deleted, cancellationToken);
    }
}
