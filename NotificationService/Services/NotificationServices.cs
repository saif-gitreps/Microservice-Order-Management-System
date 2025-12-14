using NotificationService.Interfaces;

namespace NotificationService.Services
{
    public class NotificationServices : INotificationServices
    {
        public async Task SendOrderConfirmationAsync(string userId, Guid orderId, decimal amount)
        {

            Console.WriteLine($"[EMAIL] Order Confirmation - User: {userId}, Order: {orderId}, Amount: ${amount}");
            // await _emailService.SendAsync(userEmail, "Order Confirmed", $"Your order {orderId} has been confirmed. Total: ${amount}");

            await Task.CompletedTask;
        }

        public async Task SendOrderCancellationAsync(string userId, Guid orderId, string reason)
        {
            Console.WriteLine($"[EMAIL] Order Cancelled - User: {userId}, Order: {orderId}, Reason: {reason}");
            await Task.CompletedTask;
        }

        public async Task SendPaymentFailedAsync(string userId, Guid orderId, string reason)
        {
            Console.WriteLine($"[EMAIL] Payment Failed - User: {userId}, Order: {orderId}, Reason: {reason}");
            await Task.CompletedTask;
        }

    }
}
