using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Order_Service.Dtos;
using Order_Service.Interfaces;
using System.Security.Claims;

namespace Order_Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException("User ID not found in token.");

            try
            {

                var order = await _orderService.CreateOrderAsync(userId, request);

                return CreatedAtAction(
                    nameof(GetOrder),
                    new { id = order.Id },
                    new
                    {
                        id = order.Id,
                        userId = order.UserId,
                        status = order.Status.ToString(),
                        totalAmount = order.TotalAmount,
                        createdAt = order.CreatedAt,
                        items = order.Items.Select(item => new
                        {
                            productId = item.ProductId,
                            productName = item.ProductName,
                            quantity = item.Quantity,
                            price = item.Price
                        })
                    });
            }
            catch (ArgumentException ex)
            {

                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException("User ID not found in token.");

            try
            {

                var order = await _orderService.GetOrderByIdAsync(id, userId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found." });
                }

                return Ok(new
                {
                    id = order.Id,
                    userId = order.UserId,
                    status = order.Status.ToString(),
                    totalAmount = order.TotalAmount,
                    createdAt = order.CreatedAt,
                    shippingAddress = order.ShippingAddress,
                    items = order.Items.Select(item => new
                    {
                        productId = item.ProductId,
                        productName = item.ProductName,
                        quantity = item.Quantity,
                        price = item.Price
                    })
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserOrders()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException("User ID not found in token.");

            var orders = await _orderService.GetUserOrdersAsync(userId);

            return Ok(orders.Select(order => new
            {
                id = order.Id,
                userId = order.UserId,
                status = order.Status.ToString(),
                totalAmount = order.TotalAmount,
                createdAt = order.CreatedAt,
                itemCount = order.Items.Count
            }));
        }
    }
}
