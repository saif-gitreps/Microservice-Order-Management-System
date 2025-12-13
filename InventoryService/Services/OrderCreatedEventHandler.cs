
using RabbitMQ.Client;

namespace InventoryService.Services
{
    public class OrderCreatedEventHandler: BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _queueName = "inventory_order_created_queue";
        private readonly string _exchangeName = "order_management_exchange";

        public OrderCreatedEventHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // connect to RabbitMQ  
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult(); 
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.ExchangeDeclareAsync(
                exchange: _exchangeName, 
                type: ExchangeType.Topic, 
                durable: true).GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,   
                autoDelete: false).GetAwaiter().GetResult();

            _channel.QueueBindAsync(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: "order.created").GetAwaiter().GetResult();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Create consumer to receive messages
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                try
                {
                    // Deserialize OrderCreatedEvent
                    var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);

                    if (orderCreatedEvent != null)
                    {
                        // Process event using scoped service
                        // Create scope because IInventoryService is scoped, not singleton
                        using var scope = _serviceProvider.CreateScope();
                        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

                        // Try to reserve inventory
                        var reserved = await inventoryService.ReserveInventoryForOrderAsync(
                            orderCreatedEvent.OrderId,
                            orderCreatedEvent.Items);

                        if (reserved)
                        {
                            // Inventory reserved successfully - publish success event
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
                            // Inventory reservation failed - publish failure event
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

                    // Acknowledge message processing
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    // Log error and reject message
                    // Message will be requeued or sent to dead letter queue
                    Console.WriteLine($"Error processing message: {ex.Message}");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            // Start consuming messages
            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

            // Keep service running until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }

}
