using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.TrustSafety.DTOs;
using Application.Modules.TrustSafety.Services.Interfaces;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Buyer reporting of listings, vendors, and reviews.</summary>
    [ApiController]
    [Route("api/v1")]
    public class ReportController(IReportService reports, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>Files a report against a listing, vendor, or review.</summary>
        [HttpPost("reports")]
        [Authorize]
        public async Task<ActionResult<ReportResponse>> Create([FromBody] CreateReportRequest request, CancellationToken ct)
        {
            var report = await reports.CreateAsync(currentUser.RequireUserId(), request, ct);
            return StatusCode(StatusCodes.Status201Created, report);
        }
    }
}
