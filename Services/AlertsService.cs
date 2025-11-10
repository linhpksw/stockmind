using Microsoft.Extensions.Options;
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
            _repo = repo;
            _opts = opts;
            _logger = logger;
        }
        public async Task<AlertsAggregateDto> GetAlertsAsync(CancellationToken ct)
        {
            var opts = _opts.Value ?? new AlertsOptions();
            _logger.LogInformation("Computing alerts: expiryThreshold={ExpiryThreshold}, slowWindow={Window}, slowThreshold={Thresh}",
                opts.ExpiryThresholdDays, opts.SlowMoverWindowDays, opts.SlowMoverUnitThreshold);

            // Fetch concurrently
            var lowTask = _repo.GetLowStockAsync(ct);
            var expiryTask = _repo.GetPerishableLotsExpiringWithinAsync(opts.ExpiryThresholdDays, ct);
            var soldTask = _repo.GetUnitsSoldInWindowAsync(opts.SlowMoverWindowDays, ct);

            await Task.WhenAll(lowTask, expiryTask, soldTask);

            var low = lowTask.Result.Select(AlertsMapper.ToDto).ToList();
            var expiry = expiryTask.Result.Select(AlertsMapper.ToDto).ToList();

            // Build slow movers: find products that have unitsSold < threshold OR products absent in sold list (unitsSold=0)
            var soldMap = soldTask.Result.ToDictionary(x => x.ProductId, x => x.UnitsSold);
            // Need list of all products to detect zero-sales products
            var allProducts = await _repo.GetAllProductIdsAsync(ct); // implement this in repo (lightweight)
            var slow = new List<SlowMoverDto>();
            foreach (var pid in allProducts)
            {
                soldMap.TryGetValue(pid, out var units);
                if (units < opts.SlowMoverUnitThreshold)
                    slow.Add(new SlowMoverDto(pid, opts.SlowMoverWindowDays, units));
            }

            var result = new AlertsAggregateDto
            {
                LowStock = low,
                ExpirySoon = expiry,
                SlowMovers = slow
            };

            _logger.LogInformation("Alerts computed: low={LowCount}, expiry={ExpiryCount}, slow={SlowCount}",
                result.LowStock.Count, result.ExpirySoon.Count, result.SlowMovers.Count);

            return result;
        }
    }
}
