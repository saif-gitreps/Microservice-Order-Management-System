using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using RabbitMQ.Client;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer
    .Connect("localhost:6379"));

builder.Services.AddSingleton<IModel>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var channel = connection.CreateModel();

    channel.ExchangeDeclare("order-events", ExchangeType.Topic, durable: true);
    channel.QueueDeclare("inventory-queue", durable: true, exclusive: false);
    channel.QueueDeclare("payment-queue", durable: true, exclusive: false);
    channel.QueueDeclare("notification-queue", durable: true, exclusive: false);

    channel.QueueBind("inventory-queue", "order-events", "order.created");
    channel.QueueBind("payment-queue", "order-events", "inventory.reserved");
    channel.QueueBind("notification-queue", "order-events", "payment.processed");

    return channel;
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
