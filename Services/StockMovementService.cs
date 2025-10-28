using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Helpers;
using stockmind.DTOs.Suppliers;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class StockMovementService
    {
        private readonly StockMovementRepository _stockMovementRepository;
        private readonly ILogger<StockMovementService> _logger;
        public StockMovementService(StockMovementRepository stockMovementRepository, ILogger<StockMovementService> logger)
        {
            _stockMovementRepository = stockMovementRepository;
            _logger = logger;
        }

        #region Create

        public async Task<StockMovement> CreateStockMovementAsync(StockMovement request, CancellationToken cancellationToken)
        {
            ValidateCreate(request);

            var utcNow = DateTime.UtcNow;
            var movement = new StockMovement
            {
                ProductId = request.ProductId,
                LotId = request.LotId,
                Qty = request.Qty,
                Type = request.Type.Trim(),
                RefType = request.RefType?.Trim(),
                RefId = request.RefId,
                ActorId = request.ActorId,
                Reason = request.Reason?.Trim(),
                CreatedAt = utcNow
            };

            await _stockMovementRepository.AddAsync(movement, cancellationToken);
            _logger.LogInformation("Stock Movement created with id {SupplierId}", movement.MovementId);

            return movement;
        }

        #endregion

        #region Update

        public async Task<StockMovement> UpdateStockMovementAsync(string publicId, StockMovement request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var stockMovementId = StockMovementCodeHelper.FromPublicId(publicId);
            var stockMovement = await _stockMovementRepository.GetByIdAsync(stockMovementId, cancellationToken)
                            ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });

            //await ValidateUpdateAsync(request, stockMovement.StockMovementId, cancellationToken);

            var updated = ApplyUpdates(stockMovement, request);
            await _stockMovementRepository.UpdateAsync(updated, cancellationToken);

            return updated;
        }

        private async Task ValidateUpdateAsync(StockMovement request, long stockMovementId, CancellationToken cancellationToken)
        {

        }

        private static StockMovement ApplyUpdates(StockMovement stockMovement, StockMovement request)
        {
            var utcNow = DateTime.UtcNow;

            stockMovement.MovementId = request.MovementId;

            stockMovement.ProductId = request.ProductId;

            if (request.LotId.HasValue)
            {
                stockMovement.LotId = request.LotId;
            }

            if (request.Qty != 0)
            {
                stockMovement.Qty = request.Qty;
            }

            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                stockMovement.Type = request.Type.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.RefType))
            {
                stockMovement.RefType = request.RefType.Trim();
            }

            if (request.RefId.HasValue)
            {
                stockMovement.RefId = request.RefId;
            }

            if (request.ActorId.HasValue)
            {
                stockMovement.ActorId = request.ActorId;
            }

            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                stockMovement.Reason = request.Reason.Trim();
            }

            stockMovement.CreatedAt = request.CreatedAt;

            return stockMovement;
        }

        #endregion

        #region Helpers

        private static void ValidateCreate(StockMovement request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "reason" });
            }

            if (request.Qty == 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "qty stock movement" });
            }
        }

        #endregion
    }
}
