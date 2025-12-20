using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Shared.Shared.Events.EventBus;

namespace Shared.Shared.Events.EventBus
{
    public class RabbitMQEventBus : IEventBus, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _exchangeName;

      
        public static async Task<RabbitMQEventBus> CreateAsync(
            string hostName,
            string exchangeName = "order_management_exchange")
        {
            var factory = new ConnectionFactory { HostName = hostName };
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Topic,
                durable: true);

            return new RabbitMQEventBus(connection, channel, exchangeName);
        }

      
        private RabbitMQEventBus(IConnection connection, IChannel channel, string exchangeName)
        {
            _connection = connection;
            _channel = channel;
            _exchangeName = exchangeName;
        }

        public async Task PublishAsync<T>(T @event) where T : class
        {
            var routingKey = GenerateRoutingKey<T>();
            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);
        }

        private static string GenerateRoutingKey<T>()
        {
            var typeName = typeof(T).Name;
            if (typeName.EndsWith("Event"))
            {
                typeName = typeName[..^5];
            }

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
