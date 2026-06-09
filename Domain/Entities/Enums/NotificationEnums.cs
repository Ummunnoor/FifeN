namespace Domain.Entities.Enums
{
    /// <summary>The event a notification represents; drives templating and routing.</summary>
    public enum NotificationType
    {
        VendorApproved,
        VendorRejected,
        ListingApproved,
        ListingTakenDown,
        NewReview,
        NewLead,
        ReviewNudge,
        NewInCategory,
        Security
    }

    /// <summary>Delivery channel for a notification. In-app is always recorded; WhatsApp preferred for push.</summary>
    public enum NotificationChannel
    {
        InApp,
        WhatsApp,
        Sms
    }
}
