
using Microsoft.EntityFrameworkCore;
using Order_Service.Data;
using Order_Service.Interfaces;
using Order_Service.Model;

namespace Order_Service.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;
        public OrderRepository(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateAsync(Order order)
        { 
   
            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            return await GetByIdAsync(order.Id) ?? order;
        }

        public async Task<Order?> GetByIdAsync(Guid orderId)
        { 
            return await _context.Orders.Include(o => o.Items) 
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<Order>> GetByUserIdAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
        public async Task<Order> UpdateAsync(Order order)
        {

            _context.Orders.Update(order);

            await _context.SaveChangesAsync();

            return await GetByIdAsync(order.Id) ?? order;
        }

        public async Task<List<Order>> GetByStatusAsync(OrderStatus status)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
    }
}
