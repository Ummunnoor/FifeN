namespace Domain.Entities.Enums
{
    /// <summary>
    /// Order status progression in the marketplace
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>Order created, awaiting payment</summary>
        Pending = 0,

        /// <summary>Payment received</summary>
        Paid = 1,

        /// <summary>Order has been shipped by vendor</summary>
        Shipped = 2,

        /// <summary>Order delivered to buyer</summary>
        Delivered = 3,

        /// <summary>Buyer cancelled order (before shipping)</summary>
        Cancelled = 4,

        /// <summary>Order returned by buyer</summary>
        Returned = 5
    }
}
