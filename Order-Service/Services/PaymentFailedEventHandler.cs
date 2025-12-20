using RabbitMQ.Client;

namespace Order_Service.Services
{
    public class PaymentFailedEventHandler : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _queueName = "order_payment_failed_queue";
        private readonly string _exchangeName = "order_management_exchange";

        public PaymentFailedEventHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: _queueName, exchange: _exchangeName, routingKey: "payment.failed");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var paymentFailedEvent = JsonSerializer.Deserialize<PaymentFailedEvent>(message);

                    if (paymentFailedEvent != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                        // Update order status to Cancelled
                        await orderService.UpdateOrderStatusAsync(
                            paymentFailedEvent.OrderId,
                            OrderStatus.Cancelled);
                    }

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing payment failed event: {ex.Message}");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
