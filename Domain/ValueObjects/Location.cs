using Domain.Entities.Enums;

namespace Domain.ValueObjects
{
    /// <summary>
    /// A structured Nigerian location (state + free-text city). Mapped as an EF Core owned type.
    /// The MVP filters on State then City only — no GPS, radius, or distance search.
    /// </summary>
    public sealed record Location(NigerianState State, string City);
}
