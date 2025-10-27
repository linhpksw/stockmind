using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Helpers;
using stockmind.DTOs.Grns;
using stockmind.DTOs.Suppliers;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class GrnService
    {
        private readonly GrnRepository _grnRepository;
        private readonly ProductRepository _productRepository;
        private readonly PoRepository _poRepository;
        private readonly InventoryRepository _inventoryRepository;
        private readonly LotRepository _lotRepository;
        private readonly StockMovementRepository _stockMovementRepository;
        private readonly ILogger<GrnService> _logger;

        public GrnService(
            GrnRepository grnRepository,
            ProductRepository productRepository,
            PoRepository poRepository,
            InventoryRepository inventoryRepository,
            LotRepository lotRepository,
            StockMovementRepository stockMovementRepository,
            ILogger<GrnService> logger)
        {
            _grnRepository = grnRepository;
            _productRepository = productRepository;
            _poRepository = poRepository;
            _inventoryRepository = inventoryRepository;
            _lotRepository = lotRepository;
            _stockMovementRepository = stockMovementRepository;
            _logger = logger;
        }

        #region Get by id

        public async Task<GrnResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            var grn = await _grnRepository.GetByIdAsync(id, cancellationToken);

            if (grn == null)
                return null;

            return new GrnResponseDto
            {
                Id = grn.GrnId.ToString(),
                StockMovements = grn.Grnitems
                    .Where(item => item.Lot != null)
                    .SelectMany(item => item.Lot!.StockMovements)
                    .Select(m => new StockMovementDto
                    {
                        ProductId = m.ProductId.ToString(),
                        LotId = m.LotId?.ToString() ?? string.Empty,
                        Qty = m.Qty,
                        Type = m.Type
                    })
            .ToList()
            };
        }

        #endregion

        public async Task<GrnResponseDto> CreateGrnAsync(CreateGrnRequestDto request, CancellationToken cancellationToken)
        {
            var po = await _poRepository.FindByIdAsync(request.PoId, cancellationToken)
                     ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"PO={request.PoId}" });

            var utcNow = DateTime.UtcNow;
            var grn = new Grn
            {
                PoId = po.PoId,
                ReceivedAt = request.ReceivedAt,
                CreatedAt = utcNow,
                LastModifiedAt = utcNow,
                Deleted = false
            };

            var stockMovements = new List<StockMovementDto>();

            foreach (var item in request.Items)
            {
                var product = await _productRepository.FindByIdAsync(item.ProductId, cancellationToken)
                              ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"ProductId={item.ProductId}" });

                if (product.IsPerishable && !item.ExpiryDate.HasValue)
                {
                    throw new BizException(ErrorCode4xx.InvalidInput, new[] { "ExpiryDate required for perishable product." });
                }

                // 🔹 Step 1: Update or create lot
                var lot = await _lotRepository.FindByProductIdAndLotCodeAsync(product.ProductId, item.LotCode, cancellationToken);
                if (lot == null)
                {
                    lot = new Lot
                    {
                        ProductId = product.ProductId,
                        LotCode = item.LotCode,
                        ReceivedAt = request.ReceivedAt,
                        ExpiryDate = item.ExpiryDate,
                        QtyOnHand = item.QtyReceived,
                        CreatedAt = utcNow,
                        LastModifiedAt = utcNow,
                        Deleted = false
                    };
                    await _lotRepository.AddAsync(lot, cancellationToken);
                }
                else
                {
                    lot.QtyOnHand += item.QtyReceived;
                    lot.LastModifiedAt = utcNow;
                    await _lotRepository.UpdateAsync(lot, cancellationToken);
                }

                // 🔹 Step 2: Update or create inventory
                var inventory = await _inventoryRepository.GetByProductIdAsync(product.ProductId, cancellationToken);
                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = product.ProductId,
                        OnHand = item.QtyReceived,
                        CreatedAt = utcNow,
                        LastModifiedAt = utcNow,
                        Deleted = false
                    };
                    await _inventoryRepository.AddAsync(inventory, cancellationToken);
                }
                else
                {
                    inventory.OnHand += item.QtyReceived;
                    inventory.LastModifiedAt = utcNow;
                    await _inventoryRepository.UpdateAsync(inventory, cancellationToken);
                }

                // 🔹 Step 3: Add GRN item
                var grnItem = new Grnitem
                {
                    ProductId = product.ProductId,
                    LotId = lot.LotId,
                    QtyReceived = item.QtyReceived,
                    UnitCost = item.UnitCost,
                    LotCode = item.LotCode,
                    ExpiryDate = item.ExpiryDate,
                    CreatedAt = utcNow,
                    LastModifiedAt = utcNow,
                    Deleted = false
                };
                grn.Grnitems.Add(grnItem);

                // 🔹 Step 4: Log stock movement
                var movement = new StockMovement
                {
                    ProductId = product.ProductId,
                    LotId = lot.LotId,
                    Qty = item.QtyReceived,
                    Type = "IN_RECEIPT",
                    RefType = "GRN",
                    RefId = grn.GrnId,
                    CreatedAt = utcNow
                };
                await _stockMovementRepository.AddAsync(movement, cancellationToken);

                stockMovements.Add(new StockMovementDto
                {
                    ProductId = $"PROD-{product.ProductId:D3}",
                    LotId = lot.LotCode,
                    Qty = item.QtyReceived,
                    Type = "IN_RECEIPT"
                });
            }

            await _grnRepository.AddAsync(grn, cancellationToken);
            _logger.LogInformation("GRN {GrnId} created for PO {PoId}", grn.GrnId, grn.PoId);

            return new GrnResponseDto
            {
                Id = $"GRN-{grn.GrnId:D4}",
                StockMovements = stockMovements
            };
        }
    }
}

