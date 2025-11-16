using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Responses;
using stockmind.DTOs.SalesOrders;
using stockmind.Services;

namespace stockmind.Controllers
{
    [ApiController]
    [Route("api/sales-orders")]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER,STORE_STAFF")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly SalesOrderService _salesOrderService;

        public SalesOrdersController(SalesOrderService salesOrderService)
        {
            _salesOrderService = salesOrderService;
        }

        [HttpGet("catalog")]
        public async Task<IActionResult> ListCatalogAsync(
            [FromQuery] SalesOrderCatalogQueryDto query,
            CancellationToken cancellationToken)
        {
            var page = await _salesOrderService.SearchCatalogAsync(query, cancellationToken);
            return Ok(page);
        }

        [HttpGet("next-code")]
        public async Task<IActionResult> GetNextCodeAsync(CancellationToken cancellationToken)
        {
            var seed = await _salesOrderService.GenerateSeedAsync(cancellationToken);
            return Ok(new ResponseModel<SalesOrderSeedDto>(seed));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrderAsync(
            [FromBody] CreateSalesOrderRequestDto request,
            CancellationToken cancellationToken)
        {
            var cashierId = GetCurrentUserId();
            var order = await _salesOrderService.CreateOrderAsync(request, cashierId, cancellationToken);
            return Ok(new ResponseModel<SalesOrderResponseDto>(order));
        }

        [HttpPost("pending")]
        public async Task<IActionResult> CreatePendingOrderAsync(
            [FromBody] CreateSalesOrderRequestDto request,
            CancellationToken cancellationToken)
        {
            var cashierId = GetCurrentUserId();
            var pending = await _salesOrderService.CreatePendingOrderAsync(request, cashierId, cancellationToken);
            return Ok(new ResponseModel<PendingSalesOrderResponseDto>(pending));
        }

        [AllowAnonymous]
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmPendingOrderAsync(
            [FromBody] ConfirmPendingSalesOrderRequestDto request,
            CancellationToken cancellationToken)
        {
            var order = await _salesOrderService.ConfirmPendingOrderAsync(request.Token, cancellationToken);
            return Ok(new ResponseModel<SalesOrderResponseDto>(order));
        }

        private long GetCurrentUserId()
        {
            var identifier = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue(ClaimTypes.Name)
                              ?? throw new BizException(ErrorCode4xx.Unauthorized);

            if (!long.TryParse(identifier, out var userId) || userId <= 0)
            {
                throw new BizException(ErrorCode4xx.Unauthorized);
            }

            return userId;
        }
    }
}
