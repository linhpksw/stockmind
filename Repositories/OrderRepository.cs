using Microsoft.EntityFrameworkCore;
using stockmind.DTOs.Orders;
using stockmind.Models;

namespace Application.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly StockMindDbContext _context;
    private readonly bool _blockNegativeStock = true;

    public OrderRepository(StockMindDbContext context)
    {
        _context = context;
    }

    public async Task<OrderResponse> CreateOrderAsync(OrderRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = new SalesOrder
            {
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                Deleted = false
            };
            _context.SalesOrders.Add(order);
            await _context.SaveChangesAsync();

            var movements = new List<MovementResponse>();
            var appliedLots = new List<AppliedLotResponse>();

            foreach (var item in request.Items)
            {
                var product = await _context.Products
                    .Include(p => p.Lots.Where(l => !l.Deleted && l.QtyOnHand > 0))
                    .FirstOrDefaultAsync(p => p.SkuCode == item.ProductId);

                if (product == null)
                    throw new Exception($"Product {item.ProductId} not found");

                var lots = product.IsPerishable
                    ? product.Lots
                        .Where(l => l.ExpiryDate == null || l.ExpiryDate > DateOnly.FromDateTime(DateTime.UtcNow))
                        .OrderBy(l => l.ExpiryDate)
                        .ThenBy(l => l.ReceivedAt)
                        .ToList()
                    : product.Lots
                        .OrderBy(l => l.ReceivedAt)
                        .ToList();

                var totalOnHand = lots.Sum(l => l.QtyOnHand);
                if (_blockNegativeStock && totalOnHand < item.Qty)
                    throw new Exception($"Insufficient stock for {product.SkuCode}");

                decimal remaining = item.Qty;

                foreach (var lot in lots)
                {
                    if (remaining <= 0) break;
                    var useQty = Math.Min(lot.QtyOnHand, remaining);
                    lot.QtyOnHand -= useQty;
                    remaining -= useQty;

                    var movement = new StockMovement
                    {
                        ProductId = product.ProductId,
                        LotId = lot.LotId,
                        Qty = -useQty,
                        Type = "OUT_SALE",
                        RefType = "SalesOrder",
                        RefId = order.OrderId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.StockMovements.Add(movement);

                    movements.Add(new MovementResponse
                    {
                        ProductId = product.SkuCode,
                        LotId = lot.LotCode,
                        Qty = -useQty,
                        Type = "OUT_SALE"
                    });

                    appliedLots.Add(new AppliedLotResponse
                    {
                        LotId = lot.LotCode,
                        Qty = useQty
                    });
                }

                var orderItem = new SalesOrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = product.ProductId,
                    Qty = item.Qty,
                    UnitPrice = item.UnitPrice,
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow
                };
                _context.SalesOrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new OrderResponse
            {
                Id = $"SO-{order.OrderId:D4}",
                Movements = movements,
                AppliedLots = appliedLots
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
