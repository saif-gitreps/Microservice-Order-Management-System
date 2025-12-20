using PaymentService.Interfaces;
using PaymentService.Models;
using Shared.Shared.Events.EventBus;
using Shared.Shared.Events;

namespace PaymentService.Services
{
    public class PaymentServices : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IEventBus _eventBus;

        public PaymentServices(IPaymentRepository paymentRepository, IEventBus eventBus)
        {
            _paymentRepository = paymentRepository;
            _eventBus = eventBus;
        }
        public async Task<bool> ProcessPaymentAsync(Guid orderId, string userId, decimal amount)
        {
            // Create payment record
            var payment = new Payment
            {
                OrderId = orderId,
                UserId = userId,
                Amount = amount,
                Status = PaymentStatus.Processing,
                CreatedAt = DateTime.UtcNow
            };

            payment = await _paymentRepository.CreateAsync(payment);

            // Simulating payment processing delay
            await Task.Delay(1000);

            // Simulating payment processing (randomly succeeds or fails for demo)
  
            var random = new Random();
            var success = random.Next(100) > 10; 

            if (success)
            {
        
                payment.Status = PaymentStatus.Completed;
                payment.TransactionId = $"TXN-{Guid.NewGuid()}";
                payment.ProcessedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

      
                var paymentProcessedEvent = new PaymentProcessedEvent
                {
                    OrderId = orderId,
                    UserId = userId,
                    Amount = amount,
                    TransactionId = payment.TransactionId,
                    ProcessedAt = DateTime.UtcNow
                };
                await _eventBus.PublishAsync(paymentProcessedEvent);

                return true;
            }
            else
            {
  
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = "Payment gateway declined transaction";
                payment.ProcessedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

         
                var paymentFailedEvent = new PaymentFailedEvent
                {
                    OrderId = orderId,
                    UserId = userId,
                    Reason = payment.FailureReason,
                    FailedAt = DateTime.UtcNow
                };
                await _eventBus.PublishAsync(paymentFailedEvent);

                return false;
            }
        }
    }
