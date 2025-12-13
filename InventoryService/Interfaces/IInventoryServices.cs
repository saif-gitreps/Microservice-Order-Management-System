using InventoryService.Model;
using Shared.Shared.Models;

namespace InventoryService.Interfaces
{
    public interface IInventoryServices
    {
        Task<bool> ReserveInventoryForOrderAsync(Guid orderId, List<OrderItem> items);
        Task<bool> ReleaseInventoryForOrderAsync(Guid orderId, List<OrderItem> items);
        Task<bool> ConfirmInventoryReservationAsync(Guid orderId, List<OrderItem> items);
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(Guid productId);
    }
}
