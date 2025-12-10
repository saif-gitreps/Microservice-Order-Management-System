namespace Shared
{
    public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerName,
    string CustomerEmail,
    int ProductId,
    int Quantity,
    decimal TotalAmount,
    DateTime CreatedAt
);

    public record InventoryReservedEvent(
        Guid OrderId,
        int ProductId,
        int Quantity
    );

    public record PaymentProcessedEvent(
        Guid OrderId,
        decimal Amount,
        bool Success,
        string TransactionId
    );

    public record NotificationSentEvent(
        Guid OrderId,
        string RecipientEmail,
        string NotificationType
    );

    public record CreateOrderRequest(
        string CustomerName,
        string CustomerEmail,
        int ProductId,
        int Quantity
    );

    public record OrderResponse(
        Guid OrderId,
        string CustomerName,
        string Status,
        decimal TotalAmount,
        DateTime CreatedAt
    );

    public record ProductDto(
        int Id,
        string Name,
        decimal Price,
        int Stock
    );
}