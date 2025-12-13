using InventoryService.Data;
using InventoryService.Interfaces;
using InventoryService.Model;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly InventoryDbContext _context;

        public InventoryRepository(InventoryDbContext context)
        {
            _context = context;
        }
        public async Task<bool> ConfirmReservationAsync(Guid productId, int quantity)
        {
            Inventory? inventory = await GetByProductIdAsync(productId);
            if (inventory == null || inventory.ReservedQuantity < quantity)
            {
                return false;
            }

            inventory.ReservedQuantity -= quantity;
            inventory.Quantity -= quantity;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Inventory?> GetByProductIdAsync(Guid productId)
        {
            return await _context.Inventory
           .Include(i => i.Product)
           .FirstOrDefaultAsync(i => i.ProductId == productId);
        }

        public async Task<bool> ReleaseInventoryAsync(Guid productId, int quantity)
        {
            var inventory = await GetByProductIdAsync(productId);
            if (inventory == null || inventory.ReservedQuantity < quantity)
            {
                return false;
            }

            inventory.ReservedQuantity -= quantity;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReserveInventoryAsync(Guid productId, int quantity)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Retrieve inventory with row-level locking
                // FOR UPDATE prevents concurrent modifications
                var inventory = await _context.Inventory
                    .Where(i => i.ProductId == productId)
                    .FirstOrDefaultAsync();

                if (inventory == null)
                {
                    return false;
                }

                // Check if sufficient stock is available
                // Available quantity = Total - Reserved
                if (inventory.AvailableQuantity < quantity)
                {
                    return false;
                }

                // Reserve quantity by increasing ReservedQuantity
                inventory.ReservedQuantity += quantity;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}
