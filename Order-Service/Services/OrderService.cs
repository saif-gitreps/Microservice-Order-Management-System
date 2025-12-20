using Order_Service.Interfaces;
using Shared.Shared.Events.EventBus;

namespace Order_Service.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IEventBus _eventBus;

        /// <summary>
        /// Constructor receives dependencies via dependency injection.
        /// IOrderRepository handles data access operations.
        /// IEventBus publishes events to message queue (RabbitMQ).
        /// </summary>
        public OrderService(IOrderRepository orderRepository, IEventBus eventBus)
        {
            _orderRepository = orderRepository;
            _eventBus = eventBus;
        }

        /// <summary>
        /// Creates a new order.
        /// Process:
        /// 1. Validates order data (items, quantities, prices)
        /// 2. Calculates total amount
        /// 3. Creates order entity with Pending status
        /// 4. Saves order to database
        /// 5. Publishes OrderCreatedEvent to trigger downstream processing
        /// 6. Returns created order
        /// 
        /// Event-driven flow:
        /// OrderCreatedEvent -> Inventory Service (check stock) -> Payment Service (process payment) -> Notification Service (send email)
        /// </summary>
        public async Task<Order> CreateOrderAsync(string userId, CreateOrderRequest request)
        {
            // Validate order has items
            if (request.Items == null || request.Items.Count == 0)
            {
                throw new ArgumentException("Order must contain at least one item.");
            }

            // Calculate total amount from order items
            // Sum of (quantity * price) for all items
            var totalAmount = request.Items.Sum(item => item.Quantity * item.Price);

            // Create order entity
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending, // Initial status - will be updated by events
                TotalAmount = totalAmount,
                ShippingAddress = request.ShippingAddress
            };

            // Convert request items to order item entities
            // Store product information at time of order (price, name) for historical accuracy
            foreach (var itemRequest in request.Items)
            {
                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = itemRequest.ProductId,
                    ProductName = itemRequest.ProductName,
                    Quantity = itemRequest.Quantity,
                    Price = itemRequest.Price
                };
                order.Items.Add(orderItem);
            }

            // Save order to database
            // Repository handles transaction - if save fails, nothing is persisted
            order = await _orderRepository.CreateAsync(order);

            // Publish OrderCreatedEvent to message bus
            // This event triggers downstream processing:
            // - Inventory Service: Check stock availability
            // - Payment Service: Process payment (after inventory confirmed)
            // - Notification Service: Send order confirmation email
            // 
            // Using events decouples services - Order Service doesn't need to know about Inventory/Payment services
            // Services can be developed, deployed, and scaled independently
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                CreatedAt = order.CreatedAt,
                TotalAmount = order.TotalAmount,
                Items = order.Items.Select(item => new Shared.Models.OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Price = item.Price
                }).ToList()
            };

            // Publish event asynchronously
            // Event bus handles message delivery to subscribers
            await _eventBus.PublishAsync(orderCreatedEvent);

            return order;
        }

        /// <summary>
        /// Retrieves an order by ID.
        /// Validates that the user has permission to view the order (security check).
        /// </summary>
        public async Task<Order?> GetOrderByIdAsync(Guid orderId, string userId)
        {
            // Retrieve order from database
            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null)
            {
                return null;
            }

            // Security check: ensure user can only view their own orders
            // Prevents unauthorized access to other users' orders
            if (order.UserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to view this order.");
            }

            return order;
        }

        /// <summary>
        /// Retrieves all orders for a user.
        /// Returns complete order history for user dashboard.
        /// </summary>
        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            // Repository handles querying orders by user ID
            return await _orderRepository.GetByUserIdAsync(userId);
        }

        /// <summary>
        /// Updates order status.
        /// Called when events are received from other services:
        /// - InventoryReservedEvent -> Status: Processing
        /// - PaymentProcessedEvent -> Status: Confirmed
        /// - PaymentFailedEvent -> Status: Cancelled
        /// - InventoryReservationFailedEvent -> Status: Cancelled
        /// </summary>
        public async Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
        {
            // Retrieve order from database
            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null)
            {
                throw new ArgumentException($"Order with ID {orderId} not found.");
            }

            // Update order status
            order.Status = status;

            // Save changes to database
            await _orderRepository.UpdateAsync(order);
        }
    }
}
