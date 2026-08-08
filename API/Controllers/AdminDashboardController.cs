using Application.Modules.Admin.DTOs;
using Application.Modules.Admin.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Admin home dashboard (read model).</summary>
    [ApiController]
    [Route("api/v1/admin/dashboard")]
    [Authorize(Policy = "RequireAdmin")]
    public class AdminDashboardController(IAdminDashboardService dashboard) : ControllerBase
    {
        /// <summary>Platform health and growth metrics; cross-discovery rate is the headline.</summary>
        [HttpGet]
        public async Task<ActionResult<DashboardResponse>> Get(CancellationToken ct) =>
            Ok(await dashboard.GetAsync(ct));
    }
}
