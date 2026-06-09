using System;
using Domain.Entities.Payment;

namespace Domain.Entities.Payment
{
    /// <summary>
    /// Represents a single item/line in an order.
    /// An order can contain multiple products.
    /// </summary>
    public class OrderItem
    {
        /// <summary>Primary key identifier</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Reference to the Order</summary>
        public Guid OrderId { get; set; }

        /// <summary>Navigation property to Order</summary>
        public Order? Order { get; set; }

        /// <summary>Reference to the Product being ordered</summary>
        public Guid ProductId { get; set; }

        /// <summary>Navigation property to Product</summary>
        public Domain.Entities.Product.Product? Product { get; set; }

        /// <summary>Product name at time of order (denormalized for history)</summary>
        public required string ProductName { get; set; }

        /// <summary>Unit price at time of order (denormalized for history)</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Quantity ordered</summary>
        public int Quantity { get; set; }

        /// <summary>Line total (UnitPrice * Quantity)</summary>
        public decimal LineTotal { get; set; }

        /// <summary>Product image URL at time of order (for order history display)</summary>
        public string? ProductImageUrl { get; set; }

        /// <summary>When this item was added to the order</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
