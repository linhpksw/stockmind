using Microsoft.EntityFrameworkCore;
using stockmind.DTOs.ReplenishmentSuggestion;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class ReplenishmentService
    {
        private readonly ProductRepository _productRepository;
        private readonly LotRepository _lotRepository;
        private readonly PoRepository _poRepository;
        private readonly StockMovementRepository _stockRepository;

        public ReplenishmentService(
            ProductRepository productRepository,
            LotRepository lotRepository,
            PoRepository poRepository,
            StockMovementRepository stockRepository)
        {
            _productRepository = productRepository;
            _lotRepository = lotRepository;
            _poRepository = poRepository;
            _stockRepository = stockRepository;
        }

        public async Task<List<ReplenishmentSuggestionDto>> GetSuggestionsAsync(CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow;
            var since = today.AddDays(-30);

            // Load data asynchronously
            var products = await _productRepository.GetAllAsync(cancellationToken);
            var lotBalances = await _lotRepository.Query()
                .Where(l => !l.Deleted)
                .GroupBy(l => l.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.QtyOnHand) })
                .ToListAsync(cancellationToken);
            var poItems = await _poRepository.GetOpenOrderItemsAsync(cancellationToken);
            var movements = await _stockRepository.GetSalesMovementsAsync(since, cancellationToken);

            // Sales stats: avg & sigma
            var salesStats = movements
                .GroupBy(m => new { m.ProductId, Date = m.CreatedAt.Date })
                .Select(g => new { g.Key.ProductId, DailySales = g.Sum(x => x.Qty) })
                .GroupBy(x => x.ProductId)
                .Select(g =>
                {
                    var avg = (double)g.Average(x => x.DailySales);
                    var variance = g.Average(x => Math.Pow((double)x.DailySales - avg, 2));
                    return new
                    {
                        ProductId = g.Key,
                        AvgDaily = avg,
                        SigmaDaily = Math.Sqrt(variance)
                    };
                })
                .ToDictionary(x => x.ProductId, x => x);


            // Group other info
            var inventoryMap = lotBalances.ToDictionary(i => i.ProductId, i => i.Qty);
            var onOrderMap = poItems
                .GroupBy(p => p.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.QtyOrdered));

            // Calculate ROP per product
            var results = products.Select(p =>
            {
                inventoryMap.TryGetValue(p.ProductId, out decimal onHand);
                onOrderMap.TryGetValue(p.ProductId, out decimal onOrder);
                var stats = salesStats.ContainsKey(p.ProductId) ? salesStats[p.ProductId] : null;

                var avgDaily = stats?.AvgDaily ?? 0;
                var sigmaDaily = stats?.SigmaDaily ?? 0;

                var supplierLeadTime = p.Supplier?.LeadTimeDays ?? 0;
                var normalizedLeadTime = Math.Max(0, supplierLeadTime);

                double safetyStock = p.MinStock;
                double leadTimeDemand = avgDaily * normalizedLeadTime;
                double rop = leadTimeDemand + safetyStock;
                double suggestedQty = Math.Max(0, rop - (double)onHand - (double)onOrder);

                return new ReplenishmentSuggestionDto
                {
                    ProductId = p.ProductId,
                    OnHand = onHand,
                    OnOrder = onOrder,
                    AvgDaily = Math.Round(avgDaily, 2),
                    SigmaDaily = Math.Round(sigmaDaily, 2),
                    LeadTimeDays = normalizedLeadTime,
                    SafetyStock = Math.Round(safetyStock, 2),
                    ROP = Math.Round(rop, 2),
                    SuggestedQty = Math.Round(suggestedQty, 2)
                };
            }).ToList();

            return results
                .OrderBy(r => r.SafetyStock > 0 ? (double)r.OnHand / r.SafetyStock : double.PositiveInfinity)
                .ThenBy(r => r.ProductId)
                .ToList();
        }
    }
}
