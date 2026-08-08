using Application.DTOs;
using Application.Modules.Catalog.DTOs;
using Application.Modules.Catalog.Services.Interfaces;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Admin listing moderation.</summary>
    [ApiController]
    [Route("api/v1/admin/products")]
    [Authorize(Policy = "RequireAdmin")]
    public class AdminProductController(IProductAdminService admin, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>Probation listings awaiting moderation (oldest first).</summary>
        [HttpGet("pending")]
        public async Task<ActionResult<PagedResponse<ProductSummary>>> Pending(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
            Ok(await admin.GetModerationQueueAsync(page, pageSize, ct));

        [HttpPost("{id:guid}/moderate")]
        public async Task<IActionResult> Moderate(Guid id, [FromBody] ModerateProductRequest request, CancellationToken ct)
        {
            await admin.ModerateAsync(currentUser.RequireUserId(), id, request, currentUser.IpAddress, ct);
            return Ok();
        }
    }
}
