using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Inventory;
using stockmind.Services;

namespace stockmind.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;

    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost("sync")]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
    public async Task<ActionResult<PageResponseModel<InventorySummaryDto>>> SyncAsync(
        [FromQuery] ListInventoryQueryDto query,
        CancellationToken cancellationToken)
    {
        query.Normalize();
        var snapshot = await _inventoryService.SyncSnapshotAsync(query.PageNum, query.PageSize, cancellationToken);
        return Ok(snapshot);
    }

    [HttpGet("summary")]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER,STORE_STAFF")]
    public async Task<ActionResult<PageResponseModel<InventorySummaryDto>>> GetSummaryAsync(
        [FromQuery] ListInventoryQueryDto query,
        CancellationToken cancellationToken)
    {
        query.Normalize();
        var snapshot = await _inventoryService.SyncSnapshotAsync(query.PageNum, query.PageSize, cancellationToken);
        return Ok(snapshot);
    }
}
