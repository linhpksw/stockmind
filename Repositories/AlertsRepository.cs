using Microsoft.EntityFrameworkCore;
using stockmind.Models;
using System;

namespace stockmind.Repositories
{
    public class AlertsRepository
    {
        private readonly StockMindDbContext _dbContext;

        public AlertsRepository(StockMindDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<(string ProductId, decimal OnHand, int MinStock)>> GetLowStockAsync(CancellationToken ct)
        {
            // Inventory and Product tables expected: Inventory(onHand, productId), Product(minStock)
            var q = from i in _dbContext.Inventories
                    join p in _dbContext.Products on i.ProductId equals p.ProductId
                    where i.OnHand <= p.MinStock
                    select new { p.ProductId, i.OnHand, p.MinStock };

            var list = await q.AsNoTracking().ToListAsync(ct);
            return list.Select(x => (x.ProductId.ToString(), x.OnHand, x.MinStock)).ToList();
        }

        public async Task<IReadOnlyList<(string ProductId, string LotId, DateOnly ExpiryDate, decimal QtyOnHand)>> GetPerishableLotsExpiringWithinAsync(int days, CancellationToken ct)
        {
            var cutOff = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(days));
            var q = from lot in _dbContext.Lots
                    join p in _dbContext.Products on lot.ProductId equals p.ProductId
                    where p.IsPerishable && lot.ExpiryDate != null && lot.ExpiryDate <= cutOff && lot.QtyOnHand > 0
                    select new { p.ProductId, LotId = lot.LotId, lot.ExpiryDate, lot.QtyOnHand };

            var list = await q.AsNoTracking().ToListAsync(ct);
            return list.Select(x => (x.ProductId.ToString(), x.LotId.ToString(), x.ExpiryDate!.Value, x.QtyOnHand)).ToList();
        }

        public async Task<IReadOnlyList<(string ProductId, decimal UnitsSold)>> GetUnitsSoldInWindowAsync(int windowDays, CancellationToken ct)
        {
            var fromDate = DateTime.UtcNow.AddDays(-windowDays);
            // Assuming SalesOrderItems table: ProductId, Qty, SalesOrder.CreatedAt
            var q = from si in _dbContext.SalesOrderItems
                    join so in _dbContext.SalesOrders on si.OrderId equals so.OrderId
                    where so.CreatedAt >= fromDate
                    group si by si.ProductId into g
                    select new { ProductId = g.Key, UnitsSold = g.Sum(x => x.Qty) };

            var list = await q.AsNoTracking().ToListAsync(ct);
            return list.Select(x => (x.ProductId.ToString(), x.UnitsSold)).ToList();
        }
        public async Task<IReadOnlyList<string>> GetAllProductIdsAsync(CancellationToken ct)
        {
            return await _dbContext.Products
                .Where(p => !p.Deleted)
                .Select(p => p.ProductId.ToString())
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
