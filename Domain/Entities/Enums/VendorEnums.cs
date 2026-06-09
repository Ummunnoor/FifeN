namespace Domain.Entities.Enums
{
    /// <summary>Identity verification method. BVN is intentionally excluded (BRD v2.1).</summary>
    public enum VerificationMethod
    {
        Nin,
        Cac
    }

    /// <summary>Outcome of a KYC identity verification.</summary>
    public enum VerificationStatus
    {
        Pending,
        Verified,
        Failed
    }

    /// <summary>
    /// Vendor trust tier. Probation vendors have their first three listings pre-moderated;
    /// they graduate to Trusted once those listings are approved.
    /// </summary>
    public enum TrustTier
    {
        Pending,
        Probation,
        Trusted
    }

    /// <summary>Standardized reason an admin rejects a vendor request; drives vendor-facing messaging.</summary>
    public enum RejectionReason
    {
        NameMismatch,
        UnreadableDocument,
        IneligibleBusiness,
        SuspectedFraud,
        Other
    }
}
