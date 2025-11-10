using stockmind.Commons.Attributes;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Helpers;
using stockmind.DTOs.Waste;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class WasteService
    {
        private readonly ProductRepository _productRepository;
        private readonly LotRepository _lotRepository;
        private readonly InventoryRepository _inventoryRepository;
        private readonly StockMovementService _stockMovementService;
        private readonly ILogger<WasteService> _logger;

        public WasteService(
            ProductRepository productRepository,
            LotRepository lotRepository,
            InventoryRepository inventoryRepository,
            StockMovementService stockMovementService,
            ILogger<WasteService> logger)
        {
            _productRepository = productRepository;
            _lotRepository = lotRepository;
            _inventoryRepository = inventoryRepository;
            _stockMovementService = stockMovementService;
            _logger = logger;
        }

        [Transactional]
        public async Task<WasteResponseDto> DisposeExpiredStockAsync(WasteRequestDto request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var lotCode = NormalizeRequired(request.LotId, "lotId");
            var reason = NormalizeRequired(request.Reason, "reason");
            var qty = request.Qty;

            if (qty <= 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "qty" });
            }

            var productId = ProductCodeHelper.FromPublicId(request.ProductId);
            var product = await _productRepository.FindByIdAsync(productId, cancellationToken)
                          ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { request.ProductId });

            var lot = await _lotRepository.GetLotWithHistoryAsync(product.ProductId, lotCode, cancellationToken)
                      ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { lotCode });

            if (!lot.ExpiryDate.HasValue)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "lot expiry required" });
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (lot.ExpiryDate.Value >= today)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "lot not expired yet" });
            }

            if (lot.QtyOnHand < qty)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "qty exceeds lot on hand" });
            }

            var inventory = await _inventoryRepository.GetByProductIdAsync(product.ProductId, cancellationToken)
                           ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"inventory:{request.ProductId}" });

            if (inventory.OnHand < qty)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "qty exceeds inventory on hand" });
            }

            var utcNow = DateTime.UtcNow;

            lot.QtyOnHand -= qty;
            lot.LastModifiedAt = utcNow;
            await _lotRepository.UpdateAsync(lot, cancellationToken);

            inventory.OnHand -= qty;
            inventory.LastModifiedAt = utcNow;
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

            var movement = await _stockMovementService.CreateStockMovementAsync(
                new StockMovement
                {
                    ProductId = product.ProductId,
                    LotId = lot.LotId,
                    Qty = -qty,
                    Type = "OUT_WASTE",
                    RefType = "WASTE",
                    RefId = inventory.InventoryId,
                    Reason = reason
                },
                cancellationToken);

            _logger.LogInformation(
                "Recorded waste of {Qty} units for product {Product} lot {Lot}. Movement {MovementId}",
                qty,
                request.ProductId,
                lotCode,
                movement.MovementId);

            return new WasteResponseDto
            {
                MovementId = StockMovementCodeHelper.ToPublicId(movement.MovementId),
                Type = movement.Type,
                Qty = movement.Qty
            };
        }

        private static string NormalizeRequired(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { fieldName });
            }

            return value.Trim();
        }
    }
}
