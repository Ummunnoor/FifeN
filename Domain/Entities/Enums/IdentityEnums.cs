namespace Domain.Entities.Enums
{
    /// <summary>Lifecycle state of a user account.</summary>
    public enum UserStatus
    {
        Active,
        Suspended,
        Deleted
    }

    /// <summary>Delivery channel used to send a one-time passcode.</summary>
    public enum OtpChannel
    {
        WhatsApp,
        Sms
    }

    /// <summary>Lifecycle state of a one-time passcode challenge.</summary>
    public enum OtpStatus
    {
        Pending,
        Verified,
        Expired,
        Locked
    }
}
