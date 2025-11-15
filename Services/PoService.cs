using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Responses;
using stockmind.DTOs.Pos;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services;

public class PoService
{
    private readonly PoRepository _poRepository;
    private readonly SupplierRepository _supplierRepository;
    private readonly ProductRepository _productRepository;
    private readonly MarginProfileRepository _marginProfileRepository;
    private readonly ILogger<PoService> _logger;
    private readonly Random _random = new();

    public PoService(
        PoRepository poRepository,
        SupplierRepository supplierRepository,
        ProductRepository productRepository,
        MarginProfileRepository marginProfileRepository,
        ILogger<PoService> logger)
    {
        _poRepository = poRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _marginProfileRepository = marginProfileRepository;
        _logger = logger;
    }

    #region Create

    public async Task<PoResponseDto> CreatePoAsync(CreatePoRequestDto request, CancellationToken cancellationToken)
    {

        // add mock data for products
        //var mockProduct1 = new Product
        //{
        //    SkuCode = "SKU-MOCK-001",
        //    Name = "Mock Product",
        //    CategoryId = null,
        //    IsPerishable = false,
        //    ShelfLifeDays = null,
        //    Uom = "PCS",
        //    Price = 100.00m,
        //    MinStock = 10,
        //    LeadTimeDays = 3,
        //    SupplierId = request.SupplierId,
        //    CreatedAt = DateTime.UtcNow,
        //    LastModifiedAt = DateTime.UtcNow,
        //    Deleted = false
        //};
        //await _productRepository.AddAsync(mockProduct1, cancellationToken);

        //var mockProduct2 = new Product
        //{
        //    SkuCode = "SKU-MOCK-002",
        //    Name = "Mock Product",
        //    CategoryId = null,
        //    IsPerishable = false,
        //    ShelfLifeDays = null,
        //    Uom = "PCS",
        //    Price = 100.00m,
        //    MinStock = 10,
        //    LeadTimeDays = 3,
        //    SupplierId = request.SupplierId,
        //    CreatedAt = DateTime.UtcNow,
        //    LastModifiedAt = DateTime.UtcNow,
        //    Deleted = false
        //};
        //await _productRepository.AddAsync(mockProduct2, cancellationToken);

        var supplierExists = await _supplierRepository.ExistsByIdAsync(request.SupplierId, cancellationToken);
        if (!supplierExists)
        {
            throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"SupplierId={request.SupplierId}" });
        }

        foreach (var item in request.Items)
        {
            if (!await _productRepository.ExistsByIdAsync(item.ProductId, cancellationToken))
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"ProductId={item.ProductId}" });
            }

            if (item.Qty <= 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "Quantity must be > 0" });
            }
        }

        var utcNow = DateTime.UtcNow;

        var po = new Po
        {
            SupplierId = request.SupplierId,
            Status = "OPEN",
            CreatedAt = utcNow,
            LastModifiedAt = utcNow,
            Deleted = false
        };

        foreach (var item in request.Items)
        {
            po.Poitems.Add(new Poitem
            {
                ProductId = item.ProductId,
                QtyOrdered = item.Qty,
                UnitCost = item.UnitCost,
                ExpectedDate = DateOnly.FromDateTime(item.ExpectedDate),
                CreatedAt = utcNow,
                LastModifiedAt = utcNow,
                Deleted = false
            });
        }

        await _poRepository.AddAsync(po, cancellationToken);
        _logger.LogInformation("Purchase order {PoId} created for supplier {SupplierId}", po.PoId, po.SupplierId);

        return MapToResponse(po);
    }

    #endregion

    public async Task<PageResponseModel<PurchaseOrderSummaryDto>> SyncFromMasterDataAsync(
        int pageNum,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var suppliers = await _supplierRepository.ListAllTrackedAsync(includeDeleted: false, cancellationToken);
        var products = await _productRepository.GetAllAsync(cancellationToken);
        var marginProfiles = await _marginProfileRepository.ListAsync(cancellationToken);

        await _poRepository.ClearOpenPosAsync(cancellationToken);

        var marginMap = marginProfiles.ToDictionary(
            profile => profile.ParentCategoryId,
            profile => profile.TargetMarginPct);

        var productsBySupplier = products
            .Where(product => product.SupplierId.HasValue && !product.Deleted)
            .GroupBy(product => product.SupplierId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var createdOrders = new List<PurchaseOrderSummaryDto>();
        foreach (var supplier in suppliers.Where(s => !s.Deleted))
        {
            if (!productsBySupplier.TryGetValue(supplier.SupplierId, out var supplierProducts) || supplierProducts.Count == 0)
            {
                continue;
            }

            var utcNow = DateTime.UtcNow;
            var po = new Po
            {
                SupplierId = supplier.SupplierId,
                Status = "OPEN",
                CreatedAt = utcNow,
                LastModifiedAt = utcNow,
                Deleted = false
            };

            foreach (var product in supplierProducts)
            {
                var marginPct = GetMarginPct(product, marginMap);
                var unitCost = CalculateSupplierPrice(product.Price, marginPct);
                var qty = _random.Next(10, 101);
                var leadTimeDays = Math.Max(1, supplier.LeadTimeDays);

                po.Poitems.Add(new Poitem
                {
                    ProductId = product.ProductId,
                    QtyOrdered = qty,
                    UnitCost = unitCost,
                    ExpectedDate = DateOnly.FromDateTime(utcNow.AddDays(leadTimeDays)),
                    CreatedAt = utcNow,
                    LastModifiedAt = utcNow,
                    Deleted = false,
                    Product = product
                });
            }

            await _poRepository.AddAsync(po, cancellationToken);
            createdOrders.Add(MapToSummary(po));
        }

        return await ListSummariesAsync(pageNum, pageSize, cancellationToken);
    }

    public async Task<PageResponseModel<PurchaseOrderSummaryDto>> ListSummariesAsync(
        int pageNum,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedPageNum = pageNum <= 0 ? 1 : pageNum;

        var page = await _poRepository.ListPagedAsync(normalizedPageNum, normalizedPageSize, cancellationToken);
        var items = page.Items.Select(MapToSummary).ToList();
        var total = page.Total > int.MaxValue ? int.MaxValue : (int)page.Total;

        return new PageResponseModel<PurchaseOrderSummaryDto>(normalizedPageSize, normalizedPageNum, total, items);
    }

    #region Get by ID

    public async Task<PoResponseDto> GetPoByIdAsync(long id, CancellationToken cancellationToken)
    {
        var po = await _poRepository.FindByIdAsync(id, cancellationToken)
                 ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"PO={id}" });

        if (po.Deleted)
        {
            throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { $"PO={id}" });
        }

        return MapToResponse(po);
    }

    #endregion

    #region Helpers

    private decimal GetMarginPct(Product product, IDictionary<long, decimal> marginMap)
    {
        var categoryId = product.Category?.CategoryId;
        var parentCategoryId = product.Category?.ParentCategoryId ?? categoryId;
        if (parentCategoryId.HasValue && marginMap.TryGetValue(parentCategoryId.Value, out var margin))
        {
            return margin;
        }

        return 0;
    }

    private static decimal CalculateSupplierPrice(decimal sellingPrice, decimal marginPct)
    {
        if (marginPct >= 100)
        {
            return 0;
        }

        var cost = sellingPrice - (sellingPrice * (marginPct / 100m));
        if (cost < 0)
        {
            cost = 0;
        }

        return Math.Round(cost, 2, MidpointRounding.AwayFromZero);
    }

    private static PoResponseDto MapToResponse(Po po)
    {
        return new PoResponseDto
        {
            Id = $"PO-{po.PoId:D4}",
            Status = po.Status,
            CreatedAt = po.CreatedAt
        };
    }

    private static PurchaseOrderSummaryDto MapToSummary(Po po)
    {
        var items = po.Poitems
            .OrderBy(item => item.ProductId)
            .Select(item => new PurchaseOrderItemSummaryDto
            {
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? $"Product {item.ProductId}",
                Uom = item.Product?.Uom ?? "unit",
                Qty = item.QtyOrdered,
                UnitCost = item.UnitCost,
                ExpectedDate = item.ExpectedDate,
                MediaUrl = item.Product?.MediaUrl
            })
            .ToList();

        var totalQty = items.Sum(i => i.Qty);
        var totalCost = items.Sum(i => i.Qty * i.UnitCost);

        return new PurchaseOrderSummaryDto
        {
            PoId = po.PoId,
            SupplierId = po.SupplierId,
            SupplierName = po.Supplier?.Name ?? $"Supplier {po.SupplierId}",
            Status = po.Status,
            CreatedAt = po.CreatedAt,
            TotalQty = totalQty,
            TotalCost = totalCost,
            Items = items
        };
    }

    #endregion

}
