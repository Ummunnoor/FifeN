using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Admin.DTOs;
using Application.Modules.Admin.Services.Interfaces;

namespace Application.Modules.Admin.Services.Implementations
{
    /// <summary>Serves the admin dashboard read model. All aggregation lives in the repository.</summary>
    public class AdminDashboardService(IDashboardRepository dashboard) : IAdminDashboardService
    {
        public Task<DashboardResponse> GetAsync(CancellationToken ct) => dashboard.GetDashboardAsync(ct);
    }
}
