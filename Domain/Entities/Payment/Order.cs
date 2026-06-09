using System;
using System.Collections.Generic;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Domain.Entities.Product;

namespace Domain.Entities.Payment
{
    /// <summary>
    /// Represents a customer order in the marketplace.
    /// Orders group purchases from a single shop for a buyer.
    /// </summary>
    public class Order
    {
        /// <summary>Primary key identifier</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Reference to the Shop fulfilling this order</summary>
        public Guid ShopId { get; set; }

        /// <summary>Navigation property to Shop</summary>
        public Shop? Shop { get; set; }

        /// <summary>Reference to the User (buyer) who placed the order</summary>
        public string BuyerId { get; set; } = string.Empty;

        /// <summary>Navigation property to Buyer</summary>
        public User? Buyer { get; set; }

        /// <summary>Current order status</summary>
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        /// <summary>Total order amount (sum of all items + fees)</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Payment method used for this order</summary>
        public PaymentMethodType PaymentMethod { get; set; }

        /// <summary>Additional notes from buyer (delivery instructions, etc)</summary>
        public string? BuyerNotes { get; set; }

        /// <summary>Seller's response/notes on the order</summary>
        public string? SellerNotes { get; set; }

        /// <summary>When the order was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When payment was confirmed</summary>
        public DateTime? PaidAt { get; set; }

        /// <summary>When order was marked as shipped</summary>
        public DateTime? ShippedAt { get; set; }

        /// <summary>When order was delivered</summary>
        public DateTime? DeliveredAt { get; set; }

        /// <summary>When order was cancelled (if applicable)</summary>
        public DateTime? CancelledAt { get; set; }

        /// <summary>When order was last updated</summary>
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
