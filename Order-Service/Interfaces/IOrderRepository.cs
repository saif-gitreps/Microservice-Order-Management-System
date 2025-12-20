using Order_Service.Model;

namespace Order_Service.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order> CreateAsync(Order order);
        Task<Order?> GetByIdAsync(Guid orderId);
        Task<List<Order>> GetByUserIdAsync(string userId);
        Task<Order> UpdateAsync(Order order);
        Task<List<Order>> GetByStatusAsync(OrderStatus status);
    }
}
