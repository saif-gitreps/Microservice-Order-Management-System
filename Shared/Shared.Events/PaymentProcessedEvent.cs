namespace Shared.Shared.Events
{
    public class PaymentProcessedEvent
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }
}
