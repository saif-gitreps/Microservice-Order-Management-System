using InventoryService.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Shared.Events;
using Shared.Shared.Events.EventBus;
using System.Text;
using System.Text.Json;


namespace InventoryService.Services
{
    public class OrderCreatedEventHandler : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly string _queueName = "inventory_order_created_queue";
        private readonly string _exchangeName = "order_management_exchange";
        private readonly string _hostName;

        public OrderCreatedEventHandler(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        }

        public static async Task<OrderCreatedEventHandler> CreateAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            var handler = new OrderCreatedEventHandler(serviceProvider, configuration);
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
                routingKey: "order.created");
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

                    var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);

                    if (orderCreatedEvent != null)
                    {

                        using var scope = _serviceProvider.CreateScope();
                        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryServices>();
                        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();


                        var reserved = await inventoryService.ReserveInventoryForOrderAsync(
                            orderCreatedEvent.OrderId,
                            orderCreatedEvent.Items);

                        if (reserved)
                        {

                            var reservedEvent = new InventoryReservedEvent
                            {
                                OrderId = orderCreatedEvent.OrderId,
                                UserId = orderCreatedEvent.UserId,
                                Items = orderCreatedEvent.Items,
                                ReservedAt = DateTime.UtcNow
                            };
                            await eventBus.PublishAsync(reservedEvent);
                        }
                        else
                        {

                            var failedEvent = new InventoryReservationFailedEvent
                            {
                                OrderId = orderCreatedEvent.OrderId,
                                UserId = orderCreatedEvent.UserId,
                                Reason = "Insufficient stock available",
                                FailedAt = DateTime.UtcNow
                            };
                            await eventBus.PublishAsync(failedEvent);
                        }
                    }


                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Error processing message: {ex.Message}");
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
                // Expected when stopping
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
