using Microsoft.EntityFrameworkCore;
using stockmind.Commons.Attributes;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Helpers;
using stockmind.DTOs.Inventory;
using stockmind.Models;
using stockmind.Repositories;
using stockmind.Utils;

namespace stockmind.Services
{
    public class InventoryService
    {
        private readonly InventoryRepository _inventoryRepository;
        private readonly ILogger<InventoryService> _logger;
        private readonly ProductService _productService;
        private readonly LotService _lotService;
        private readonly StockMovementService _movementService;
        private readonly IMapperUtil _mapper;
        public InventoryService(
            InventoryRepository inventoryRepository,
            ILogger<InventoryService> logger,
            ProductService productService,
            LotService lotService,
            StockMovementService movementService,
            IMapperUtil mapper)
        {
            _inventoryRepository = inventoryRepository;
            _logger = logger;
            _productService = productService;
            _lotService = lotService;
            _movementService = movementService;
            _mapper = mapper;
        }

        [Transactional]
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

        public async Task<InventoryLedgerDto> GetStockLedgerAsync(string productId, CancellationToken ct)
        {
            // Defensive validation
            if (string.IsNullOrWhiteSpace(productId))
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "productId is required." });

            _logger.LogInformation("Fetching stock ledger for product {ProductId}", productId);

            // Load inventory summary (onHand), lots, and recent movements in repository layer
            var inventory = await GetInventoryByProductIdAsync(productId, false, ct);

            var lots = await _lotService.GetLotsByProductAsync(productId, false, ct);
            var recentMovements = await _movementService.GetRecentMovementsAsync(productId, count: 50, false, ct); // N configurable; here 50

            // Map to DTOs
            var lotDtos = lots.Select(l => _mapper.MapToLotDto(l)).ToList();
            var movementDtos = recentMovements.Select(m => _mapper.MapToMovementDto(m)).ToList();

            // Business rule check: sum(lots.qtyOnHand) must equal onHand
            var sumLots = lotDtos.Sum(l => l.QtyOnHand);
            if (sumLots != inventory.OnHand)
            {
                _logger.LogWarning("Data inconsistency detected for product {ProductId}: onHand={OnHand} sumLots={SumLots}",
                    productId, inventory.OnHand, sumLots);

                // Throw a specific business/data exception so caller can react.
                throw new BizException(ErrorCode4xx.InventoryLotMismatch,
                    new[] { $"Inventory mismatch for product {productId}: onHand={inventory.OnHand} vs sum(lots)={sumLots}." });

            }


            var result = new InventoryLedgerDto(
                ProductId: productId,
                OnHand: inventory.OnHand,
                Lots: lotDtos,
                RecentMovements: movementDtos
            );

            _logger.LogInformation("Successfully built ledger for {ProductId} (lots={Count}, movements={MvCount})",
                productId, lotDtos.Count, movementDtos.Count);

            return result;
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

        [Transactional]
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
