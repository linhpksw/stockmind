using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Customers;
using stockmind.Services;

namespace stockmind.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN,INVENTORY_MANAGER,STORE_STAFF,CASHIER")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    public CustomersController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> LookupByPhoneAsync([FromQuery] string phoneNumber, CancellationToken cancellationToken)
    {
        var customer = await _customerService.LookupByPhoneAsync(phoneNumber, cancellationToken);
        return Ok(new ResponseModel<CustomerResponseDto?>(customer));
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomerAsync(
        [FromBody] CreateCustomerRequestDto request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new ResponseModel<CustomerResponseDto>(customer));
    }
}
