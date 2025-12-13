using InventoryService.Model;

namespace InventoryService.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid productId);
        Task<List<Product>> GetAllAsync();
        Task<Product> CreateAsync(Product product);
    }
}
