using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Admin.DTOs;

namespace Application.Modules.Admin.Services.Interfaces
{
    /// <summary>Admin home dashboard read model (30-day rolling windows per BRD §9).</summary>
    public interface IAdminDashboardService
    {
        Task<DashboardResponse> GetAsync(CancellationToken ct);
    }

    /// <summary>Aggregated read queries powering the admin dashboard.</summary>
    public interface IDashboardRepository
    {
        Task<DashboardResponse> GetDashboardAsync(CancellationToken ct);
    }
}
