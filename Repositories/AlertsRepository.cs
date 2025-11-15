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
            var lotBalances = from lot in _dbContext.Lots
                              where !lot.Deleted
                              group lot by lot.ProductId
                into g
                              select new { ProductId = g.Key, OnHand = g.Sum(l => l.QtyOnHand) };

            var query = from product in _dbContext.Products
                        join balance in lotBalances on product.ProductId equals balance.ProductId into grouped
                        from balance in grouped.DefaultIfEmpty()
                        let onHand = balance != null ? balance.OnHand : 0m
                        where onHand <= product.MinStock
                        select new { product.ProductId, OnHand = onHand, product.MinStock };

            var list = await query.AsNoTracking().ToListAsync(ct);
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
