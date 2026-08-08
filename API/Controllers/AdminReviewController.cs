using Application.Modules.Engagement.DTOs;
using Application.Modules.Engagement.Services.Interfaces;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Admin review moderation.</summary>
    [ApiController]
    [Route("api/v1/admin/reviews")]
    [Authorize(Policy = "RequireAdmin")]
    public class AdminReviewController(IReviewAdminService admin, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>Hides, removes, or restores a review (audited).</summary>
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Moderate(Guid id, [FromBody] ModerateReviewRequest request, CancellationToken ct)
        {
            await admin.ModerateAsync(currentUser.RequireUserId(), id, request, currentUser.IpAddress, ct);
            return Ok();
        }
    }
}
