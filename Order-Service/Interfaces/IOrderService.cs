using Order_Service.Dtos;
using Order_Service.Model;

namespace Order_Service.Interfaces
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(string userId, CreateOrderRequest request);
        Task<Order?> GetOrderByIdAsync(Guid orderId, string userId);
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
    }
}
