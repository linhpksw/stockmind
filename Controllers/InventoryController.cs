using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Inventory;
using stockmind.Services;

namespace stockmind.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryService _inventoryService;
        public InventoryController(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpPost("adjustments")]
        public async Task<IActionResult> AdjustInventory([FromBody] InventoryAdjustmentRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _inventoryService.AdjustInventoryAsync(request, cancellationToken);
            return CreatedAtAction(nameof(AdjustInventory), new { id = result.MovementId }, new ResponseModel<InventoryAdjustmentResponseDto>(result));
        }
    }
}
