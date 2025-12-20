namespace PaymentService.Interfaces
{
    public interface IPaymentService
    {
        Task<bool> ProcessPaymentAsync(Guid orderId, string userId, decimal amount);
    }
}
