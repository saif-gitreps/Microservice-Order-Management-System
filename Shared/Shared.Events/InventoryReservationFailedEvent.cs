namespace Shared.Shared.Events
{
    public class InventoryReservationFailedEvent
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime FailedAt { get; set; }
    }
}
