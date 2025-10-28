using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Helpers;
using stockmind.DTOs.Inventory;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class InventoryService
    {
        private readonly InventoryRepository _inventoryRepository;
        private readonly ILogger<InventoryService> _logger;
        private readonly ProductService _productService;
        private readonly LotService _lotService;
        private readonly StockMovementService _movementService;
        public InventoryService(
            InventoryRepository inventoryRepository,
            ILogger<InventoryService> logger,
            ProductService productService,
            LotService lotService,
            StockMovementService movementService)
        {
            _inventoryRepository = inventoryRepository;
            _logger = logger;
            _productService = productService;
            _lotService = lotService;
            _movementService = movementService;
        }

        public async Task<InventoryAdjustmentResponseDto> AdjustInventoryAsync(InventoryAdjustmentRequestDto req, CancellationToken cancellationToken)
        {
            // ✅ Step 1: Validate input
            if (req.QtyDelta == 0)
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "Quantity delta must not be zero." });
            if (string.IsNullOrWhiteSpace(req.Reason))
                throw new BizNotFoundException(ErrorCode4xx.MissingRequiredParameter, new[] { "Reason is required." });

            // ✅ Step 2: Get product via ProductService
            var productId = ProductCodeHelper.FromPublicId(req.ProductId);
            var lotId = LotCodeHelper.FromPublicId(req.LotId);
            var product = await _productService.GetProductByIdAsync(req.ProductId, false, cancellationToken);

            if (product.IsPerishable && req.LotId == null)
                throw new BizException(ErrorCode4xx.MissingRequiredParameter, new[] { "Perishable items require lotId for traceability." });

            Lot? lot = null;
            if (req.LotId != null)
            {
                // ✅ Step 3: Get and validate lot via LotService
                lot = await _lotService.GetLotForProductAsync(req.LotId, req.ProductId, true, cancellationToken);

                if (lot.ExpiryDate.HasValue && lot.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) < DateTime.UtcNow &&
                    !string.Equals(req.Reason, "waste", StringComparison.OrdinalIgnoreCase))
                    throw new BizException(ErrorCode4xx.InvalidInput, new[] { "Cannot adjust expired lot unless reason='waste'." });
            }

            // ✅ Step 4: Transactional update
            if (req.LotId != null && lot != null)
            {
                lot.QtyOnHand += req.QtyDelta;
                await _lotService.UpdateLotAsync(req.LotId, lot, cancellationToken);
            }

            var inv = await GetInventoryByProductIdAsync(req.ProductId, false, cancellationToken);

            inv.OnHand += req.QtyDelta;
            UpdateInventoryAsync(InventoryCodeHelper.ToPublicId(inv.InventoryId), inv, cancellationToken).Wait();

            var movement = new StockMovement
            {
                ProductId = productId,
                LotId = lotId,
                Qty = req.QtyDelta,
                Type = "ADJUSTMENT",
                RefType = "INVENTORY",
                RefId = inv.InventoryId,
                ActorId = req.ActorId,
                Reason = req.Reason,
                CreatedAt = DateTime.UtcNow
            };
            await _movementService.CreateStockMovementAsync(movement, cancellationToken);

            return new InventoryAdjustmentResponseDto
            {
                MovementId = StockMovementCodeHelper.ToPublicId(movement.MovementId),
                Qty = movement.Qty
            };
        }

        #region Get by id

        public async Task<Inventory> GetInventoryByIdAsync(string publicId, bool includeDeleted, CancellationToken cancellationToken)
        {
            var inventoryId = InventoryCodeHelper.FromPublicId(publicId);

            var inventory = await _inventoryRepository.GetByIdAsync(inventoryId, cancellationToken)
                             ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });

            if (inventory.Deleted && !includeDeleted)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });
            }

            return inventory;
        }

        #endregion

        #region Get by product id

        public async Task<Inventory> GetInventoryByProductIdAsync(string publicId, bool includeDeleted, CancellationToken cancellationToken)
        {
            var productId = ProductCodeHelper.FromPublicId(publicId);

            var inventory = await _inventoryRepository.GetByProductIdAsync(productId, cancellationToken)
                             ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });

            if (inventory.Deleted && !includeDeleted)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });
            }

            return inventory;
        }

        #endregion

        #region Update

        public async Task<Inventory> UpdateInventoryAsync(string publicId, Inventory request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var inventoryId = InventoryCodeHelper.FromPublicId(publicId);
            var inventory = await _inventoryRepository.GetByIdAsync(inventoryId, cancellationToken)
                            ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });

            if (inventory.Deleted)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });
            }

            //await ValidateUpdateAsync(request, inventory.InventoryId, cancellationToken);

            var updated = ApplyUpdates(inventory, request);
            await _inventoryRepository.UpdateAsync(updated, cancellationToken);

            return updated;
        }

        //private async Task ValidateUpdateAsync(Inventory request, long inventoryId, CancellationToken cancellationToken)
        //{
        //    if (request.LeadTimeDays.HasValue && request.LeadTimeDays.Value < 0)
        //    {
        //        throw new BizException(ErrorCode4xx.InvalidInput, new[] { "leadTimeDays" });
        //    }

        //    if (!string.IsNullOrWhiteSpace(request.Name))
        //    {
        //        var normalizedName = request.Name.Trim();
        //        if (await _inventoryRepository.ExistsByNameAsync(normalizedName, inventoryId, cancellationToken))
        //        {
        //            throw new BizDataAlreadyExistsException(ErrorCode4xx.DataAlreadyExists, new[] { normalizedName });
        //        }
        //    }
        //}

        private static Inventory ApplyUpdates(Inventory inventory, Inventory request)
        {
            var utcNow = DateTime.UtcNow;

            inventory.ProductId = request.ProductId;
            inventory.OnHand = request.OnHand;

            inventory.LastModifiedAt = utcNow;
            return inventory;
        }

        #endregion

    }
}
