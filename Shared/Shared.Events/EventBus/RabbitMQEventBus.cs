using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Shared.Shared.Events.EventBus;

namespace Shared.Shared.Events.EventBus
{
    public class RabbitMQEventBus: IEventBus, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _exchangeName;

        public RabbitMQEventBus(string hostName, string exchangeName = "order_management_exchange")
        {
            _exchangeName = exchangeName;
            var factory = new ConnectionFactory { HostName = hostName };

            // this is a TCP connection to RabbitMQ
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();

            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
        }

 
        public async Task PublishAsync<T>(T @event) where T : class
        {

            var routingKey = GenerateRoutingKey<T>();

            // Serialize event to JSON for transmission
            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            // Create properties for the message
            var properties = _channel.;
            properties.Persistent = true; // Message survives broker restart
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            await Task.CompletedTask;
        }

        private static string GenerateRoutingKey<T>()
        {
            var typeName = typeof(T).Name;

            // Remove "Event" suffix if present
            if (typeName.EndsWith("Event"))
            {
                typeName = typeName[..^5];
            }

            // Convert PascalCase to lowercase with dots
            // Example: OrderCreated -> order.created
            var result = string.Empty;
            for (int i = 0; i < typeName.Length; i++)
            {
                if (char.IsUpper(typeName[i]) && i > 0)
                {
                    result += ".";
                }
                result += char.ToLower(typeName[i]);
            }

            return result;
        }

        public void Dispose()
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
