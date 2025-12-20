using PaymentService.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Shared.Events;
using System.Text;
using System.Text.Json;

namespace PaymentService.Services
{
    public class InventoryReservedEventHandler : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly string _queueName = "payment_inventory_reserved_queue";
        private readonly string _exchangeName = "order_management_exchange";
        private readonly string _hostName;

        public InventoryReservedEventHandler(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        }

        public static async Task<InventoryReservedEventHandler> CreateAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            var handler = new InventoryReservedEventHandler(serviceProvider, configuration);
            await handler.InitializeAsync();
            return handler;
        }

        private async Task InitializeAsync()
        {
            var factory = new ConnectionFactory { HostName = _hostName };
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true);

            await _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await _channel.QueueBindAsync(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: "inventory.reserved");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("Channel not initialized. Use CreateAsync factory method.");
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
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

                        var totalAmount = inventoryReservedEvent.Items.Sum(item => item.Quantity * item.Price);

                        await paymentService.ProcessPaymentAsync(
                            inventoryReservedEvent.OrderId,
                            inventoryReservedEvent.UserId,
                            totalAmount);
                    }

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing payment: {ex.Message}");
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
            }
        }

        public override async void Dispose()
        {
            if (_channel != null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
            }

            if (_connection != null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
            }

            base.Dispose();
        }
    }
}