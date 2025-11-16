using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.SalesOrders;
using stockmind.Services;

namespace stockmind.Controllers;

[ApiController]
[Route("api/sales-orders")]
public class SalesOrdersController : ControllerBase
{
    private readonly SalesOrderService _salesOrderService;

    public SalesOrdersController(SalesOrderService salesOrderService)
    {
        _salesOrderService = salesOrderService;
    }

    [HttpGet("context")]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,STORE_STAFF,CASHIER")]
    public async Task<IActionResult> GetContextAsync(CancellationToken cancellationToken)
    {
        var (userId, fullName) = ResolveCurrentUser();
        var context = await _salesOrderService.GetContextAsync(userId, fullName, cancellationToken);
        return Ok(new ResponseModel<SalesOrderContextDto>(context));
    }

    [HttpGet("available-items")]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,STORE_STAFF,CASHIER")]
    public async Task<IActionResult> SearchSellableLotsAsync(
        [FromQuery] SellableLotQueryDto filters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filters.SearchTerm) && Request.Query.TryGetValue("query", out var rawQuery))
        {
            filters.SearchTerm = rawQuery.ToString();
        }

        var items = await _salesOrderService.SearchSellableLotsAsync(filters, cancellationToken);
        return Ok(new ResponseModel<IReadOnlyList<SellableLotDto>>(items));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,STORE_STAFF,CASHIER")]
    public async Task<IActionResult> CreateSalesOrderAsync(
        [FromBody] CreateSalesOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var (userId, fullName) = ResolveCurrentUser();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var response = await _salesOrderService.CreateSalesOrderAsync(
            request,
            userId,
            fullName,
            baseUrl,
            cancellationToken);
        return Ok(new ResponseModel<CreateSalesOrderResponseDto>(response));
    }

    [HttpGet("pending/{token:guid}/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmPendingOrderAsync([FromRoute] Guid token, CancellationToken cancellationToken)
    {
        var summary = await _salesOrderService.ConfirmPendingOrderAsync(token, cancellationToken);
        return Ok(new ResponseModel<SalesOrderSummaryDto>(summary));
    }

    [HttpGet("pending/{id:long}")]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,STORE_STAFF,CASHIER")]
    public async Task<IActionResult> GetPendingStatusAsync([FromRoute] long id, CancellationToken cancellationToken)
    {
        var status = await _salesOrderService.GetPendingStatusAsync(id, cancellationToken);
        return Ok(new ResponseModel<PendingSalesOrderStatusDto>(status));
    }

    private (long userId, string fullName) ResolveCurrentUser()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!long.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException("Unable to resolve the current user.");
        }

        var fullName = User.FindFirstValue("fullName") ?? User.Identity?.Name ?? "Cashier";
        return (userId, fullName);
    }
}
