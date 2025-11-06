using stockmind.DTOs.ReplenishmentSuggestion;
using stockmind.Repositories;

namespace stockmind.Services {
    public class ReplenishmentService {
        private readonly ProductRepository _productRepository;
        private readonly InventoryRepository _inventoryRepository;
        private readonly PoRepository _poRepository;
        private readonly StockMovementRepository _stockRepository;

        public ReplenishmentService(
            ProductRepository productRepository,
            InventoryRepository inventoryRepository,
            PoRepository poRepository,
            StockMovementRepository stockRepository) {
            _productRepository = productRepository;
            _inventoryRepository = inventoryRepository;
            _poRepository = poRepository;
            _stockRepository = stockRepository;
        }

        public async Task<List<ReplenishmentSuggestionDto>> GetSuggestionsAsync(CancellationToken cancellationToken) {
            const double Z = 1.28;
            var today = DateTime.UtcNow;
            var since = today.AddDays(-30);

            // Load data asynchronously
            var products = await _productRepository.GetAllAsync(cancellationToken);
            var inventories = await _inventoryRepository.GetAllAsync(cancellationToken);
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
            var inventoryMap = inventories.ToDictionary(i => i.ProductId, i => i.OnHand);
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

                double safetyStock = Z * sigmaDaily * Math.Sqrt(p.LeadTimeDays);
                double leadTimeDemand = avgDaily * p.LeadTimeDays;
                double rop = leadTimeDemand + safetyStock;
                double suggestedQty = Math.Max(0, rop - (double)onHand - (double)onOrder);

                return new ReplenishmentSuggestionDto
                {
                    ProductId = p.ProductId,
                    OnHand = onHand,
                    OnOrder = onOrder,
                    AvgDaily = Math.Round(avgDaily, 2),
                    SigmaDaily = Math.Round(sigmaDaily, 2),
                    LeadTimeDays = p.LeadTimeDays,
                    SafetyStock = Math.Round(safetyStock, 2),
                    ROP = Math.Round(rop, 2),
                    SuggestedQty = Math.Round(suggestedQty, 2)
                };
            }).ToList();

            return results;
        }
    }
}
