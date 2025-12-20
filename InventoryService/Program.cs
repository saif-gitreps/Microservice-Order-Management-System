using InventoryService.Data;
using InventoryService.Interfaces;
using InventoryService.Repositories;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Shared.Events.EventBus;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=inventoryservice_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(connectionString));

var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));

var rabbitMqHost = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";

var eventBus = await RabbitMQEventBus.CreateAsync(rabbitMqHost);
builder.Services.AddSingleton<IEventBus>(eventBus);

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryServices, InventoryServices>();

// This service listens to OrderCreatedEvent and processes inventory reservations
builder.Services.AddHostedService<OrderCreatedEventHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    dbContext.Database.EnsureCreated();

    // Seed initial inventory data for testing
    SeedInventoryData(dbContext);
}


app.Run();

// for development and testing purposes
void SeedInventoryData(InventoryDbContext context)
{
    if (!context.Products.Any())
    {
        var products = new[]
        {
            new InventoryService.Model.Product { Id = Guid.NewGuid(), Name = "Laptop", Description = "High-performance laptop", Price = 999.99m },
            new InventoryService.Model.Product { Id = Guid.NewGuid(), Name = "Mouse", Description = "Wireless mouse", Price = 29.99m },
            new InventoryService.Model.Product { Id = Guid.NewGuid(), Name = "Keyboard", Description = "Mechanical keyboard", Price = 79.99m }
        };

        context.Products.AddRange(products);

        foreach (var product in products)
        {
            context.Inventory.Add(new InventoryService.Model.Inventory
            {
                ProductId = product.Id,
                Quantity = 100, // Initial stock
                ReservedQuantity = 0
            });
        }

        context.SaveChanges();
    }
}

