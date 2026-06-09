using System.Collections.Generic;

namespace Application.Modules.Admin.DTOs
{
    /// <summary>
    /// The admin home dashboard. "Active" figures use 30-day rolling windows (BRD §9), and the
    /// cross-discovery rate is the north-star headline metric.
    /// </summary>
    public record DashboardResponse(
        int RegisteredUsers,
        int ActiveUsers30d,
        int ActiveListings,
        double OtpSuccessRate,
        int Interactions30d,
        double CrossDiscoveryRate,
        IReadOnlyList<NameCount> TopCategories,
        IReadOnlyList<NameCount> TopLocations,
        int PendingApprovals,
        int OpenReports);

    /// <summary>A named tally row, e.g. a category or location with its live-listing count.</summary>
    public record NameCount(string Name, int Count);
}
