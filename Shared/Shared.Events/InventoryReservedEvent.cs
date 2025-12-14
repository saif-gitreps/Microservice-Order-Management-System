using Shared.Shared.Models;

namespace Shared.Shared.Events
{
    public class InventoryReservedEvent
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new();
        public DateTime ReservedAt { get; set; }
    }
}
