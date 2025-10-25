using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Suppliers;
using stockmind.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace stockmind.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly SupplierService _supplierService;

    public SuppliersController(SupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    #region Create

    [HttpPost]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
    public async Task<IActionResult> CreateSupplierAsync([FromBody] CreateSupplierRequestDto request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.CreateSupplierAsync(request, cancellationToken);
        return CreatedAtRoute(
         routeName: "GetSupplierById",
         routeValues: new { id = supplier.Id },
         value: new ResponseModel<SupplierResponseDto>(supplier)
     );
    }

    #endregion

    #region Get by id

    [HttpGet("{id}", Name = "GetSupplierById")]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER,STORE_STAFF")]
    public async Task<IActionResult> GetSupplierByIdAsync(
        [FromRoute] string id,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierService.GetSupplierByIdAsync(id, includeDeleted, cancellationToken);
        return Ok(new ResponseModel<SupplierResponseDto>(supplier));
    }

    #endregion

    #region List

    [HttpGet]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER,STORE_STAFF")]
    public async Task<IActionResult> ListSuppliersAsync([FromQuery] ListSuppliersQueryDto query, CancellationToken cancellationToken)
    {
        var page = await _supplierService.ListSuppliersAsync(query, cancellationToken);
        return Ok(page);
    }

    #endregion

    #region Update

    [HttpPatch("{id}")]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
    public async Task<IActionResult> UpdateSupplierAsync([FromRoute] string id, [FromBody] UpdateSupplierRequestDto request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.UpdateSupplierAsync(id, request, cancellationToken);
        return Ok(new ResponseModel<SupplierResponseDto>(supplier));
    }

    #endregion

    #region Soft delete

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
    public async Task<IActionResult> SoftDeleteSupplierAsync([FromRoute] string id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.SoftDeleteSupplierAsync(id, cancellationToken);
        return Ok(new ResponseModel<SupplierResponseDto>(supplier));
    }

    #endregion
}
