
using RabbitMQ.Client;

namespace PaymentService.Services
{
    public class InventoryReservedEventHandler : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _queueName = "payment_inventory_reserved_queue";
        private readonly string _exchangeName = "order_management_exchange";

        public InventoryReservedEventHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: _queueName, exchange: _exchangeName, routingKey: "inventory.reserved");
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
                    var inventoryReservedEvent = JsonSerializer.Deserialize<InventoryReservedEvent>(message);

                    if (inventoryReservedEvent != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

                        // Calculate total amount from items
                        var totalAmount = inventoryReservedEvent.Items.Sum(item => item.Quantity * item.Price);

                        // Process payment
                        await paymentService.ProcessPaymentAsync(
                            inventoryReservedEvent.OrderId,
                            inventoryReservedEvent.UserId,
                            totalAmount);
                    }

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing payment: {ex.Message}");
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
