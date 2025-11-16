using System;
using System.Collections.Generic;
using System.Linq;
using stockmind.Commons.Attributes;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Helpers;
using stockmind.Commons.Responses;
using stockmind.DTOs.Grns;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class GrnService
    {
        private readonly GrnRepository _grnRepository;
        private readonly ProductRepository _productRepository;
        private readonly PoRepository _poRepository;
        private readonly LotRepository _lotRepository;
        private readonly StockMovementRepository _stockMovementRepository;
        private readonly ILogger<GrnService> _logger;

        public GrnService(
            GrnRepository grnRepository,
            ProductRepository productRepository,
            PoRepository poRepository,
            LotRepository lotRepository,
            StockMovementRepository stockMovementRepository,
            ILogger<GrnService> logger)
        {
            _grnRepository = grnRepository;
            _productRepository = productRepository;
            _poRepository = poRepository;
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
        [Transactional]
        public virtual async Task<GrnResponseDto> CreateGrnAsync(CreateGrnRequestDto request, CancellationToken cancellationToken)
        {
            var (grn, stockMovements) = await CreateGrnInternalAsync(request, cancellationToken);
            return new GrnResponseDto
            {
                Id = $"GRN-{grn.GrnId:D4}",
                StockMovements = stockMovements
            };
        }

        public async Task<PageResponseModel<GrnSummaryDto>> SyncFromOpenPurchaseOrdersAsync(
            int pageNum,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var openPos = await _poRepository.GetOpenPosWithItemsAsync(cancellationToken);
            foreach (var po in openPos)
            {
                var request = BuildRequestFromPo(po);
                await CreateGrnInternalAsync(request, cancellationToken);

                po.Status = "RECEIVED";
                po.LastModifiedAt = DateTime.UtcNow;
            }

            if (openPos.Count > 0)
            {
                await _poRepository.SaveChangesAsync(cancellationToken);
            }

            return await ListSummariesAsync(pageNum, pageSize, cancellationToken);
        }

        public async Task<PageResponseModel<GrnSummaryDto>> ListSummariesAsync(
            int pageNum,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            var normalizedPageNum = pageNum <= 0 ? 1 : pageNum;

            var pendingSummaries = (await _poRepository.GetOpenPosWithItemsAsync(cancellationToken))
                .Select(MapPendingPoToSummary)
                .OrderByDescending(summary => summary.ReceivedAt)
                .ToList();
            var pendingCount = pendingSummaries.Count;

            var pageStart = (normalizedPageNum - 1) * normalizedPageSize;
            var results = new List<GrnSummaryDto>(normalizedPageSize);
            var pendingAdded = 0;

            if (pageStart < pendingCount)
            {
                var pendingItems = pendingSummaries
                    .Skip(pageStart)
                    .Take(normalizedPageSize)
                    .ToList();
                results.AddRange(pendingItems);
                pendingAdded = pendingItems.Count;
            }

            var remaining = normalizedPageSize - pendingAdded;
            var receivedSkip = Math.Max(0, pageStart - pendingCount);

            GrnRepository.PageResult<Grn> receivedPage;
            if (remaining > 0 || pageStart >= pendingCount)
            {
                receivedPage = await _grnRepository.ListPagedWithItemsAsync(receivedSkip, remaining, cancellationToken);
                if (remaining > 0 && receivedPage.Items.Count > 0)
                {
                    results.AddRange(receivedPage.Items.Select(grn => MapToSummary(grn, null, null)));
                }
            }
            else
            {
                receivedPage = await _grnRepository.ListPagedWithItemsAsync(0, 0, cancellationToken);
            }

            var total = pendingCount + receivedPage.Total;
            var totalCapped = total > int.MaxValue ? int.MaxValue : (int)total;

            return new PageResponseModel<GrnSummaryDto>(normalizedPageSize, normalizedPageNum, totalCapped, results);
        }

        [Transactional]
        public virtual async Task<GrnSummaryDto> AcceptPoAsync(long poId, CancellationToken cancellationToken)
        {
            var po = await _poRepository.FindByIdAsync(poId, cancellationToken)
                     ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"PO={poId}" });

            if (po.Deleted)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"PO={poId}" });
            }

            if (!string.Equals(po.Status, "OPEN", StringComparison.OrdinalIgnoreCase))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "Only open purchase orders can be accepted." });
            }

            var request = BuildRequestFromPo(po);
            var (grn, _) = await CreateGrnInternalAsync(request, cancellationToken);

            po.Status = "RECEIVED";
            po.LastModifiedAt = DateTime.UtcNow;
            await _poRepository.SaveChangesAsync(cancellationToken);

            return MapToSummary(grn, po, request);
        }

        [Transactional]
        public virtual async Task<GrnSummaryDto> CancelPoAsync(long poId, CancellationToken cancellationToken)
        {
            var po = await _poRepository.FindByIdAsync(poId, cancellationToken)
                     ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"PO={poId}" });

            if (po.Deleted)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"PO={poId}" });
            }

            if (!string.Equals(po.Status, "OPEN", StringComparison.OrdinalIgnoreCase))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "Only open purchase orders can be cancelled." });
            }

            po.Status = "CANCELLED";
            po.LastModifiedAt = DateTime.UtcNow;
            await _poRepository.SaveChangesAsync(cancellationToken);

            return MapPendingPoToSummary(po);
        }

        private async Task<(Grn grn, List<StockMovementDto> stockMovements)> CreateGrnInternalAsync(
            CreateGrnRequestDto request,
            CancellationToken cancellationToken)
        {
            var po = await _poRepository.FindByIdAsync(request.PoId, cancellationToken)
                     ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"PO={request.PoId}" });

            var utcNow = DateTime.UtcNow;
            var receivedAt = request.ReceivedAt == default ? utcNow : request.ReceivedAt;
            var grn = new Grn
            {
                PoId = po.PoId,
                ReceivedAt = receivedAt,
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

                var lot = await _lotRepository.FindByProductIdAndLotCodeAsync(product.ProductId, item.LotCode, cancellationToken);
                if (lot == null)
                {
                    lot = new Lot
                    {
                        ProductId = product.ProductId,
                        LotCode = item.LotCode,
                        ReceivedAt = receivedAt,
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

            return (grn, stockMovements);
        }

        private CreateGrnRequestDto BuildRequestFromPo(Po po)
        {
            var utcNow = DateTime.UtcNow;
            var items = po.Poitems.Select(item =>
            {
                var product = item.Product;
                var lotCode = $"LOT-{po.PoId}-{product?.ProductId ?? item.ProductId}-{Guid.NewGuid():N}".Substring(0, 20);
                DateOnly? expiry = null;
                if (product?.IsPerishable == true && product.ShelfLifeDays.HasValue && product.ShelfLifeDays.Value > 0)
                {
                    expiry = DateOnly.FromDateTime(utcNow.AddDays(product.ShelfLifeDays.Value));
                }

                return new CreateGrnItemDto
                {
                    ProductId = item.ProductId,
                    QtyReceived = item.QtyOrdered,
                    UnitCost = item.UnitCost,
                    LotCode = lotCode,
                    ExpiryDate = expiry
                };
            }).ToList();

            return new CreateGrnRequestDto
            {
                PoId = po.PoId,
                ReceivedAt = utcNow,
                Items = items
            };
        }

        private GrnSummaryDto MapToSummary(Grn grn, Po? po, CreateGrnRequestDto? request)
        {
            var supplierName = po?.Supplier?.Name
                               ?? grn.Grnitems.Select(i => i.Product?.Supplier?.Name)
                                   .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
                               ?? "Supplier";

            long supplierId;
            if (po != null)
            {
                supplierId = po.SupplierId;
            }
            else
            {
                var linkedSupplierIdNullable = grn.Grnitems
                    .Select(i => i.Product?.SupplierId)
                    .FirstOrDefault(id => id.HasValue);

                supplierId = linkedSupplierIdNullable ?? 0L;
            }
            var items = request != null
                ? request.Items.Select(item =>
                        new GrnItemSummaryDto
                        {
                            ProductId = item.ProductId,
                            ProductName = po?.Poitems.FirstOrDefault(p => p.ProductId == item.ProductId)?.Product?.Name ?? $"Product {item.ProductId}",
                            QtyReceived = item.QtyReceived,
                            UnitCost = item.UnitCost,
                            LotCode = item.LotCode,
                            ExpiryDate = item.ExpiryDate,
                            ExpectedDate = null,
                            MediaUrl = po?.Poitems.FirstOrDefault(p => p.ProductId == item.ProductId)?.Product?.MediaUrl
                        })
                    .ToList()
                : grn.Grnitems.Select(item => new GrnItemSummaryDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? $"Product {item.ProductId}",
                    QtyReceived = item.QtyReceived,
                    UnitCost = item.UnitCost,
                    LotCode = item.LotCode,
                    ExpiryDate = item.ExpiryDate,
                    ExpectedDate = null,
                    MediaUrl = item.Product?.MediaUrl
                }).ToList();

            var totalQty = items.Sum(i => i.QtyReceived);
            var totalCost = items.Sum(i => i.QtyReceived * i.UnitCost);

            return new GrnSummaryDto
            {
                GrnId = grn.GrnId,
                PoId = grn.PoId ?? 0,
                SupplierId = supplierId,
                SupplierName = supplierName,
                ReceivedAt = grn.ReceivedAt,
                Status = po?.Status ?? grn.Po?.Status ?? "RECEIVED",
                TotalQty = totalQty,
                TotalCost = totalCost,
                Items = items
            };
        }

        private GrnSummaryDto MapPendingPoToSummary(Po po)
        {
            var expectedDate = po.Poitems
                .Select(item => item.ExpectedDate.HasValue
                    ? item.ExpectedDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null)
                .Where(date => date.HasValue)
                .DefaultIfEmpty(null)
                .Min();

            var items = po.Poitems.Select(item => new GrnItemSummaryDto
            {
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? $"Product {item.ProductId}",
                QtyReceived = item.QtyOrdered,
                UnitCost = item.UnitCost,
                LotCode = null,
                ExpiryDate = null,
                ExpectedDate = item.ExpectedDate,
                MediaUrl = item.Product?.MediaUrl
            }).ToList();

            var totalQty = items.Sum(i => i.QtyReceived);
            var totalCost = items.Sum(i => i.QtyReceived * i.UnitCost);

            var fallbackReceivedAt = po.LastModifiedAt == default ? po.CreatedAt : po.LastModifiedAt;

            return new GrnSummaryDto
            {
                GrnId = po.PoId,
                PoId = po.PoId,
                SupplierId = po.SupplierId,
                SupplierName = po.Supplier?.Name ?? "Supplier",
                ReceivedAt = expectedDate ?? fallbackReceivedAt,
                Status = string.IsNullOrWhiteSpace(po.Status) ? "OPEN" : po.Status,
                TotalQty = totalQty,
                TotalCost = totalCost,
                Items = items
            };
        }
    }
}

