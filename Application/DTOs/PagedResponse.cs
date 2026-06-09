using System.Collections.Generic;

namespace Application.DTOs
{
    /// <summary>A single page of results with the totals needed for pagination controls.</summary>
    public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
}
