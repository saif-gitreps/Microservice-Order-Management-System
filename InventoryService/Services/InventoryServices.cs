using InventoryService.Interfaces;
using InventoryService.Model;
using Shared.Shared.Models;

namespace InventoryService.Services
{
    public class InventoryServices : IInventoryServices
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IProductRepository _productRepository;

        public InventoryServices(IInventoryRepository inventoryRepository, 
            IProductRepository productRepository)
        {
            _inventoryRepository = inventoryRepository;
            _productRepository = productRepository;
        }   

        public async Task<bool> ConfirmInventoryReservationAsync(Guid orderId, List<OrderItem> items)
        {
            bool allConfirmed = true;
            foreach (var item in items)
            {
                var confirmed = await _inventoryRepository.ConfirmReservationAsync(item.ProductId, item.Quantity);
                if (!confirmed)
                {
                    allConfirmed = false;
                }
            }
            return allConfirmed;
        }

        public async Task<Product?> GetProductAsync(Guid productId)
        {
            return await _productRepository.GetByIdAsync(productId);
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            return await _productRepository.GetAllAsync();
        }

        public async Task<bool> ReleaseInventoryForOrderAsync(Guid orderId, List<OrderItem> items)
        {
            bool allReleased = true;
            foreach (var item in items)
            {
                var released = await _inventoryRepository.ReleaseInventoryAsync(item.ProductId, item.Quantity);
                if (!released)
                {
                    allReleased = false;
                }
            }
            return allReleased;
        }

        public async Task<bool> ReserveInventoryForOrderAsync(Guid orderId, List<OrderItem> items)
        {
            foreach (var item in items)
            {
                var inventory = await _inventoryRepository.GetByProductIdAsync(item.ProductId);
                if (inventory == null || inventory.AvailableQuantity < item.Quantity)
                {
                    return false;
                }
            }

            foreach (var item in items)
            {
                var reserved = await _inventoryRepository.ReserveInventoryAsync(item.ProductId, item.Quantity);
                if (!reserved)
                {
                    // If reservation fails, release any previously reserved , This ensures atomicity - either all items are reserved or none
                    await ReleaseInventoryForOrderAsync(orderId, items.TakeWhile(i => i != item).ToList());
                    return false;
                }
            }

            return true;
        }
    }
}
