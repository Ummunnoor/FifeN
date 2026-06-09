using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Vendors.DTOs;
using Application.Modules.Vendors.Services.Interfaces;
using Application.Services.Interfaces;
using Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Admin vendor moderation. Every mutation is audited.</summary>
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Policy = "RequireAdmin")]
    public class AdminVendorController(IVendorAdminService admin, ICurrentUserService currentUser) : ControllerBase
    {
        [HttpGet("vendor-requests")]
        public async Task<ActionResult<IReadOnlyList<VendorRequestQueueItem>>> GetQueue(
            [FromQuery] VendorRequestStatus status = VendorRequestStatus.Pending,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
            Ok(await admin.GetQueueAsync(status, page, pageSize, ct));

        [HttpPost("vendor-requests/{id:guid}/approve")]
        public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        {
            await admin.ApproveAsync(currentUser.RequireUserId(), id, currentUser.IpAddress, ct);
            return Ok();
        }

        [HttpPost("vendor-requests/{id:guid}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectVendorRequestRequest request, CancellationToken ct)
        {
            await admin.RejectAsync(currentUser.RequireUserId(), id, request.Reason, currentUser.IpAddress, ct);
            return Ok();
        }

        [HttpPost("vendors/{id:guid}/suspend")]
        public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendVendorRequest request, CancellationToken ct)
        {
            await admin.SuspendAsync(currentUser.RequireUserId(), id, request.Reason, currentUser.IpAddress, ct);
            return Ok();
        }

        [HttpPost("vendors/{id:guid}/reinstate")]
        public async Task<IActionResult> Reinstate(Guid id, CancellationToken ct)
        {
            await admin.ReinstateAsync(currentUser.RequireUserId(), id, currentUser.IpAddress, ct);
            return Ok();
        }
    }
}
