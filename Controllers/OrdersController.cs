using Application.Repositories;
using Microsoft.AspNetCore.Mvc;
using stockmind.DTOs.Orders;

namespace Application.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;

    public OrdersController(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderRequest request)
    {
        try
        {
            var result = await _orderRepository.CreateOrderAsync(request);
            return Created("", result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
