namespace Domain.ValueObjects
{
    /// <summary>
    /// A monetary amount in a given currency. Mapped as an EF Core owned type.
    /// Defaults to Nigerian Naira (NGN), the only currency used in the MVP.
    /// </summary>
    public sealed record Money(decimal Amount, string Currency = "NGN");
}
