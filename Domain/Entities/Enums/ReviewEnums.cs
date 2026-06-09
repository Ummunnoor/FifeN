namespace Domain.Entities.Enums
{
    /// <summary>Moderation visibility of a review. Only <see cref="Visible"/> reviews are shown publicly.</summary>
    public enum ReviewStatus
    {
        Visible,
        Hidden,
        Removed
    }
}
