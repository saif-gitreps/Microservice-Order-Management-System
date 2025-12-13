using InventoryService.Model;

namespace InventoryService.Interfaces
{
    public interface IInventoryRepository
    {
        Task<Inventory?> GetByProductIdAsync(Guid productId);
        Task<bool> ReserveInventoryAsync(Guid productId, int quantity);
        Task<bool> ReleaseInventoryAsync(Guid productId, int quantity);
        Task<bool> ConfirmReservationAsync(Guid productId, int quantity);
    }
}
