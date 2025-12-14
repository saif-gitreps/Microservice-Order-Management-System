using Shared.Shared.Models;

namespace Shared.Shared.Events
{
    public class OrderCreatedEvent
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }
}
