namespace Domain.Entities.Enums
{
    /// <summary>How a listing's price should be interpreted by buyers.</summary>
    public enum PriceType
    {
        Fixed,
        Negotiable,
        ContactForPrice
    }

    /// <summary>Condition of a listed item. Sealed/New only apply to consumable categories.</summary>
    public enum ProductCondition
    {
        New,
        Used,
        Sealed
    }

    /// <summary>
    /// Visibility lifecycle of a listing. Public reads only ever see <see cref="Live"/>.
    /// Unavailable = out of stock (reactivatable); Archived = vendor removed; Removed = admin takedown.
    /// </summary>
    public enum ListingStatus
    {
        Live,
        Unavailable,
        Archived,
        Removed
    }
}
