using System;
using Domain.Entities.Enums;

namespace Domain.Entities.Payment
{
    /// <summary>
    /// Represents payment transaction details for an order.
    /// Enables audit trail for payment tracking and reconciliation.
    /// </summary>
    public class PaymentTransaction
    {
        /// <summary>Primary key identifier</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Reference to the Order</summary>
        public Guid OrderId { get; set; }

        /// <summary>Navigation property to Order</summary>
        public Order? Order { get; set; }

        /// <summary>Payment method used</summary>
        public PaymentMethodType PaymentMethod { get; set; }

        /// <summary>Amount paid</summary>
        public decimal Amount { get; set; }

        /// <summary>External transaction reference (bank confirmation, etc)</summary>
        public string? TransactionReference { get; set; }

        /// <summary>Proof of payment (image URL, receipt, etc)</summary>
        public string? ProofUrl { get; set; }

        /// <summary>Payment status: Pending, Confirmed, Failed, Refunded</summary>
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Failed, Refunded

        /// <summary>Additional notes (reason for failure, etc)</summary>
        public string? Notes { get; set; }

        /// <summary>When the payment was initiated</summary>
        public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the payment was confirmed</summary>
        public DateTime? ConfirmedAt { get; set; }

        /// <summary>When payment was last updated</summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
