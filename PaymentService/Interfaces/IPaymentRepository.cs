using PaymentService.Models;

namespace PaymentService.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment?> GetByOrderIdAsync(Guid orderId);
        Task<Payment> UpdateAsync(Payment payment);
    }
}
