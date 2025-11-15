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
                .Select(lot =>
                {
                    var latestGrnItem = lot.Grnitems
                        .OrderByDescending(item => item.CreatedAt)
                        .FirstOrDefault();

                    var latestReceivedAt = latestGrnItem?.CreatedAt ?? lot.ReceivedAt;

                    return new
                    {
                        Lot = lot,
                        LatestGrnItem = latestGrnItem,
                        LatestReceivedAt = latestReceivedAt
                    };
                })
                .OrderByDescending(entry => entry.LatestReceivedAt)
                .ThenByDescending(entry => entry.Lot.LotId)
                .ThenBy(entry => entry.Lot.ExpiryDate ?? DateOnly.MaxValue)
                .Select(entry => new InventoryLotSummaryDto
                {
                    LotId = entry.Lot.LotId,
                    LotCode = entry.Lot.LotCode,
                    ReceivedAt = entry.LatestReceivedAt,
                    ExpiryDate = entry.Lot.ExpiryDate,
                    QtyOnHand = entry.Lot.QtyOnHand,
                    UnitCost = entry.LatestGrnItem?.UnitCost ?? 0
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
