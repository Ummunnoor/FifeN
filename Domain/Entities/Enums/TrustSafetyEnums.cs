namespace Domain.Entities.Enums
{
    /// <summary>The kind of object a buyer report targets (polymorphic target).</summary>
    public enum ReportTargetType
    {
        Listing,
        Vendor,
        Review
    }

    /// <summary>Reason supplied when reporting content.</summary>
    public enum ReportReason
    {
        Prohibited,
        Counterfeit,
        Scam,
        Offensive,
        Spam,
        WrongCategory,
        Other
    }

    /// <summary>Lifecycle of a report in the moderation queue.</summary>
    public enum ReportStatus
    {
        Open,
        Actioned,
        Dismissed
    }
}
