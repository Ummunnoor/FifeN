namespace Domain.Entities.Enums
{
    /// <summary>
    /// Available payment methods in the marketplace
    /// Tailored for Nigerian traders and low-connectivity scenarios
    /// </summary>
    public enum PaymentMethodType
    {
        /// <summary>Cash on delivery - most common in Nigeria</summary>
        CashOnDelivery = 0,

        /// <summary>Bank transfer (most common for merchants)</summary>
        BankTransfer = 1,

        /// <summary>Mobile money (MTN MoMo, Airtel Money, etc)</summary>
        MobileTransfer = 2,

        /// <summary>eWallet services (future expansion)</summary>
        eWallet = 3,

        /// <summary>In-app wallet/credit</summary>
        InAppBalance = 4
    }
}
