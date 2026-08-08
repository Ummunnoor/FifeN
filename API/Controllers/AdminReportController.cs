using Application.DTOs;
using Application.Modules.TrustSafety.DTOs;
using Application.Modules.TrustSafety.Services.Interfaces;
using Application.Services.Interfaces;
using Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Admin report moderation queue and resolution.</summary>
    [ApiController]
    [Route("api/v1/admin/reports")]
    [Authorize(Policy = "RequireAdmin")]
    public class AdminReportController(IReportAdminService admin, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>The moderation queue, optionally filtered by status.</summary>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<ReportResponse>>> Queue(
            [FromQuery] ReportStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
            Ok(await admin.GetQueueAsync(status, page, pageSize, ct));

        /// <summary>Resolves a report (Actioned or Dismissed); audited.</summary>
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveReportRequest request, CancellationToken ct)
        {
            await admin.ResolveAsync(currentUser.RequireUserId(), id, request, currentUser.IpAddress, ct);
            return Ok();
        }
    }
}
