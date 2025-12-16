using NotificationService.Interfaces;
using NotificationService.Services;
using Shared.Shared.Events.EventBus;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var rabbitMqHost = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
builder.Services.AddSingleton<IEventBus>(sp => new RabbitMQEventBus(rabbitMqHost));

// Register notification service
builder.Services.AddScoped<INotificationServices, NotificationServices>();

// Services that listen to events to send notifications
builder.Services.AddHostedService<PaymentProcessedEventHandler>();
builder.Services.AddHostedService<PaymentFailedEventHandler>();
builder.Services.AddHostedService<InventoryReservationFailedEventHandler>();

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
