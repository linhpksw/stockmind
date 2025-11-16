using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Customers;
using stockmind.Services;

namespace stockmind.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER,STORE_STAFF")]
    public class CustomersController : ControllerBase
    {
        private readonly CustomerService _customerService;

        public CustomersController(CustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchAsync([FromQuery] CustomerSearchQueryDto query, CancellationToken cancellationToken)
        {
            var customers = await _customerService.SearchByPhoneAsync(query, cancellationToken);
            return Ok(new ResponseModel<IReadOnlyList<CustomerSummaryDto>>(customers));
        }

        [HttpPost("quick-enroll")]
        public async Task<IActionResult> QuickEnrollAsync(
            [FromBody] QuickEnrollCustomerRequestDto request,
            CancellationToken cancellationToken)
        {
            var customer = await _customerService.QuickEnrollAsync(request, cancellationToken);
            return Ok(new ResponseModel<CustomerSummaryDto>(customer));
        }
    }
}
