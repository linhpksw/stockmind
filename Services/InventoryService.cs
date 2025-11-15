using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using stockmind.Commons.Responses;
using stockmind.DTOs.Inventory;
using stockmind.Repositories;

namespace stockmind.Services;

public class InventoryService
{
    private readonly ProductRepository _productRepository;
    private readonly LotRepository _lotRepository;

    public InventoryService(ProductRepository productRepository, LotRepository lotRepository)
    {
        _productRepository = productRepository;
        _lotRepository = lotRepository;
    }

    public async Task<PageResponseModel<InventorySummaryDto>> SyncSnapshotAsync(
        int pageNum,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedPageNum = pageNum <= 0 ? 1 : pageNum;

        var baseQuery = _productRepository.Query()
            .Where(p => !p.Deleted)
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .OrderBy(p => p.Name);

        var page = await _productRepository.ListAsync(baseQuery, normalizedPageNum, normalizedPageSize, cancellationToken);
        var productIds = page.Items.Select(p => p.ProductId).ToList();

        var lots = await _lotRepository.Query()
            .Where(lot => !lot.Deleted && productIds.Contains(lot.ProductId))
            .Include(lot => lot.Grnitems)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var lotsByProduct = lots
            .GroupBy(lot => lot.ProductId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var summaries = new List<InventorySummaryDto>(page.Items.Count);

        foreach (var product in page.Items)
        {
            lotsByProduct.TryGetValue(product.ProductId, out var productLots);
            var lotDtos = (productLots ?? new List<Models.Lot>())
                .OrderBy(lot => lot.ExpiryDate)
                .ThenBy(lot => lot.ReceivedAt)
                .Select(lot => new InventoryLotSummaryDto
                {
                    LotId = lot.LotId,
                    LotCode = lot.LotCode,
                    ReceivedAt = lot.ReceivedAt,
                    ExpiryDate = lot.ExpiryDate,
                    QtyOnHand = lot.QtyOnHand,
                    UnitCost = lot.Grnitems.OrderByDescending(item => item.CreatedAt).FirstOrDefault()?.UnitCost ?? 0
                })
                .ToList();

            summaries.Add(new InventorySummaryDto
            {
                ProductId = product.ProductId,
                SkuCode = product.SkuCode,
                Name = product.Name,
                CategoryName = product.Category?.Name,
                SupplierName = product.Supplier?.Name,
                Uom = product.Uom,
                MediaUrl = product.MediaUrl,
                OnHand = lotDtos.Sum(lot => lot.QtyOnHand),
                Lots = lotDtos
            });
        }

        var total = page.Total > int.MaxValue ? int.MaxValue : (int)page.Total;
        return new PageResponseModel<InventorySummaryDto>(normalizedPageSize, normalizedPageNum, total, summaries);
    }
}
