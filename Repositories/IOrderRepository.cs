using stockmind.DTOs.Orders;

namespace Application.Repositories;

public interface IOrderRepository
{
    Task<OrderResponse> CreateOrderAsync(OrderRequest request);
}
