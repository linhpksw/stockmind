using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Responses;
using stockmind.DTOs.Inventory;
using stockmind.Models;
using stockmind.Services;

namespace stockmind.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryService _inventoryService;
        private readonly ILogger<InventoryController> _logger;
        public InventoryController(InventoryService inventoryService, ILogger<InventoryController> logger)
        {
            _inventoryService = inventoryService;
            _logger = logger;
        }

        [HttpPost("adjustments")]
        public async Task<IActionResult> AdjustInventory([FromBody] InventoryAdjustmentRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _inventoryService.AdjustInventoryAsync(request, cancellationToken);
                return CreatedAtAction(nameof(AdjustInventory), new { id = result.MovementId }, new ResponseModel<InventoryAdjustmentResponseDto>(result));
            }
            catch (BizNotFoundException nf) // replace with your project's not-found exception
            {
                _logger.LogInformation(nf, "Not Found");
                return NotFound(new { message = nf.Message });
            }
            catch (BizException be) // replace with your project's business exception base
            {
                _logger.LogWarning(be, "Business error");
                return BadRequest(new { message = be.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adjusting inventory");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        [HttpGet("{productId}")]
        public async Task<IActionResult> GetLedger([FromRoute] string productId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                _logger.LogWarning("GET /inventory called with empty productId");
                return BadRequest(new { message = "productId is required" });
            }

            try
            {
                var ledger = await _inventoryService.GetStockLedgerAsync(productId, ct);
                return Ok(ledger);
            }
            catch (BizNotFoundException nf)
            {
                _logger.LogInformation(nf, "Product not found: {ProductId}", productId);
                return NotFound(new { message = nf.Message });
            }
            catch (BizException be)
            {
                _logger.LogWarning(be, "Business error while fetching ledger for {ProductId}", productId);
                return BadRequest(new { message = be.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching ledger for {ProductId}", productId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
