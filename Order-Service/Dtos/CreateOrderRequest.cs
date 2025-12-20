using Order_Service.Interfaces;

namespace Order_Service.Dtos
{
    public class CreateOrderRequest
    {
        public List<OrderItemRequest> Items { get; set; } = new();
        public string? ShippingAddress { get; set; }
    }

}
}
