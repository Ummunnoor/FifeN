using System;
using Domain.Entities.Enums;

namespace Application.Modules.Discovery.DTOs
{
    /// <summary>
    /// Buyer-facing search/browse parameters. Location filtering is structured State→City only
    /// (no GPS/radius). <see cref="Sort"/> is one of: recent, price_asc, price_desc, rating.
    /// </summary>
    public record ProductQuery(
        string? Q = null,
        Guid? CategoryId = null,
        NigerianState? State = null,
        string? City = null,
        decimal? MinPrice = null,
        decimal? MaxPrice = null,
        ProductCondition? Condition = null,
        string Sort = "recent",
        int Page = 1,
        int PageSize = 20);
}
