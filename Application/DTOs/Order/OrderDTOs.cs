
namespace Application.DTOs.Order
{
    /// <summary>
    /// DTO for a single order item (product in order)
    /// </summary>
    public class OrderItemDTO
    {
        public Guid ProductId { get; set; }
        public required string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
        public string? ProductImageUrl { get; set; }
    }

    /// <summary>
    /// DTO for creating a new order
    /// </summary>
    public class CreateOrderDTO
    {
        /// <summary>Shop identifier (which vendor to order from)</summary>
        public Guid ShopId { get; set; }

        /// <summary>List of items in the order</summary>
        public required List<CreateOrderItemDTO> Items { get; set; }

        /// <summary>Total amount for the order</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Payment method selected</summary>
        public PaymentMethodType PaymentMethod { get; set; }

        /// <summary>Special delivery instructions from buyer</summary>
        public string? BuyerNotes { get; set; }
    }

    /// <summary>
    /// DTO for creating order items
    /// </summary>
    public class CreateOrderItemDTO
    {
        /// <summary>Product identifier</summary>
        public Guid ProductId { get; set; }

        /// <summary>Quantity to order</summary>
        public int Quantity { get; set; }
    }

    /// <summary>
    /// DTO for retrieving order details
    /// </summary>
    public class GetOrderDTO
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public string? ShopName { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public PaymentMethodType PaymentMethod { get; set; }
        public List<OrderItemDTO> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? BuyerNotes { get; set; }
        public string? SellerNotes { get; set; }
    }

    /// <summary>
    /// DTO for updating order status (seller operation)
    /// </summary>
    public class UpdateOrderStatusDTO
    {
        public Guid OrderId { get; set; }
        public OrderStatus NewStatus { get; set; }
        public string? SellerNotes { get; set; }
    }

    /// <summary>
    /// DTO for cancelling an order (buyer operation)
    /// </summary>
    public class CancelOrderDTO
    {
        public Guid OrderId { get; set; }
        public string? CancellationReason { get; set; }
    }
}
