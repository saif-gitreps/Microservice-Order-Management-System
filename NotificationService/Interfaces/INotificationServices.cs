namespace NotificationService.Interfaces
{
    public interface INotificationServices
    {
        Task SendOrderConfirmationAsync(string userId, Guid orderId, decimal amount);
        Task SendOrderCancellationAsync(string userId, Guid orderId, string reason);
        Task SendPaymentFailedAsync(string userId, Guid orderId, string reason);
    }
}
}
