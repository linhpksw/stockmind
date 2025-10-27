using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.DTOs.Pos;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services;

public class PoService
{
    private readonly PoRepository _poRepository;
    private readonly SupplierRepository _supplierRepository;
    private readonly ProductRepository _productRepository;
    private readonly ILogger<PoService> _logger;

    public PoService(
        PoRepository poRepository,
        SupplierRepository supplierRepository,
        ProductRepository productRepository,
        ILogger<PoService> logger)
    {
        _poRepository = poRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
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

    private static PoResponseDto MapToResponse(Po po)
    {
        return new PoResponseDto
        {
            Id = $"PO-{po.PoId:D4}",
            Status = po.Status,
            CreatedAt = po.CreatedAt
        };
    }

    #endregion

}
