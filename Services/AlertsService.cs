using Microsoft.Extensions.Options;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.DTOs.Alert;
using stockmind.Repositories;
using stockmind.Utils;

namespace stockmind.Services
{
    public class AlertsService
    {
        private readonly AlertsRepository _repo;
        private readonly IOptions<AlertsOptions> _opts;
        private readonly ILogger<AlertsService> _logger;

        public AlertsService(AlertsRepository repo, IOptions<AlertsOptions> opts, ILogger<AlertsService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AlertsAggregateDto> GetAlertsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var options = BuildEffectiveOptions();
            _logger.LogInformation(
                "Computing alerts with ExpiryThresholdDays={ExpiryThresholdDays}, SlowMoverWindowDays={SlowMoverWindowDays}, SlowMoverUnitThreshold={SlowMoverUnitThreshold}",
                options.ExpiryThresholdDays,
                options.SlowMoverWindowDays,
                options.SlowMoverUnitThreshold);

            var aggregate = new AlertsAggregateDto
            {
                LowStock = await BuildLowStockAlertsAsync(ct),
                ExpirySoon = await BuildExpirySoonAlertsAsync(options.ExpiryThresholdDays, ct),
                SlowMovers = await BuildSlowMoverAlertsAsync(options, ct)
            };

            _logger.LogInformation(
                "Alerts computed successfully: lowStockCount={LowStockCount}, expiryCount={ExpiryCount}, slowMoversCount={SlowMoversCount}",
                aggregate.LowStock.Count,
                aggregate.ExpirySoon.Count,
                aggregate.SlowMovers.Count);

            return aggregate;
        }

        private AlertsOptions BuildEffectiveOptions()
        {
            var options = _opts.Value ?? new AlertsOptions();
            ValidateOptions(options);
            return options;
        }

        private static void ValidateOptions(AlertsOptions options)
        {
            if (options.ExpiryThresholdDays < 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "Alerts:ExpiryThresholdDays must be greater than or equal to zero." });
            }

            if (options.SlowMoverWindowDays <= 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "Alerts:SlowMoverWindowDays must be greater than zero." });
            }

            if (options.SlowMoverUnitThreshold < 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "Alerts:SlowMoverUnitThreshold must be greater than or equal to zero." });
            }
        }

        private async Task<List<LowStockDto>> BuildLowStockAlertsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogDebug("Fetching low stock records.");

            var records = await _repo.GetLowStockAsync(ct)
                          ?? Array.Empty<(string ProductId, decimal OnHand, int MinStock)>();

            var lowStock = records
                .Where(record => !string.IsNullOrWhiteSpace(record.ProductId))
                .Select(AlertsMapper.ToDto)
                .ToList();

            _logger.LogDebug("Low stock alert calculation finished. Count={LowStockCount}", lowStock.Count);
            return lowStock;
        }

        private async Task<List<ExpirySoonDto>> BuildExpirySoonAlertsAsync(int expiryThresholdDays, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogDebug("Fetching expiry alerts with threshold {ExpiryThresholdDays} days.", expiryThresholdDays);

            var records = await _repo.GetPerishableLotsExpiringWithinAsync(expiryThresholdDays, ct)
                          ?? Array.Empty<(string ProductId, string LotId, DateOnly ExpiryDate, decimal QtyOnHand)>();

            var expiryAlerts = records
                .Where(record => !string.IsNullOrWhiteSpace(record.ProductId) && !string.IsNullOrWhiteSpace(record.LotId))
                .Select(AlertsMapper.ToDto)
                .ToList();

            _logger.LogDebug("Expiry alert calculation finished. Count={ExpiryCount}", expiryAlerts.Count);
            return expiryAlerts;
        }

        private async Task<List<SlowMoverDto>> BuildSlowMoverAlertsAsync(AlertsOptions options, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogDebug("Fetching slow mover datasets for window {WindowDays}.", options.SlowMoverWindowDays);

            var soldStats = await _repo.GetUnitsSoldInWindowAsync(options.SlowMoverWindowDays, ct);
            ct.ThrowIfCancellationRequested();

            var products = await _repo.GetAllProductIdsAsync(ct);
            ct.ThrowIfCancellationRequested();

            var soldMap = soldStats
                ?.Where(record => !string.IsNullOrWhiteSpace(record.ProductId))
                .GroupBy(record => record.ProductId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.UnitsSold), StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            var allProducts = products
                ?.Where(pid => !string.IsNullOrWhiteSpace(pid))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? [];

            var slowMovers = new List<SlowMoverDto>(allProducts.Count);
            foreach (var productId in allProducts)
            {
                ct.ThrowIfCancellationRequested();
                soldMap.TryGetValue(productId, out var unitsSold);
                if (unitsSold < options.SlowMoverUnitThreshold)
                {
                    slowMovers.Add(AlertsMapper.ToDto((productId, unitsSold), options.SlowMoverWindowDays));
                }
            }

            _logger.LogDebug("Slow mover alert calculation finished. Count={SlowMoverCount}", slowMovers.Count);
            return slowMovers;
        }
    }
}
