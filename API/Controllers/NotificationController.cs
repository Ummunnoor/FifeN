using Application.DTOs;
using Application.Modules.Notifications.DTOs;
using Application.Modules.Notifications.Services.Interfaces;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>The caller's in-app notification centre.</summary>
    [ApiController]
    [Route("api/v1/notifications")]
    [Authorize]
    public class NotificationController(INotificationFeedService notifications, ICurrentUserService currentUser)
        : ControllerBase
    {
        /// <summary>The caller's notifications, newest first.</summary>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<NotificationResponse>>> List(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
            Ok(await notifications.GetFeedAsync(currentUser.RequireUserId(), page, pageSize, ct));

        /// <summary>The caller's unread count, for a badge.</summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<UnreadCountResponse>> UnreadCount(CancellationToken ct) =>
            Ok(new UnreadCountResponse(await notifications.GetUnreadCountAsync(currentUser.RequireUserId(), ct)));

        /// <summary>Marks one notification read (owner only).</summary>
        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
        {
            await notifications.MarkReadAsync(currentUser.RequireUserId(), id, ct);
            return NoContent();
        }

        /// <summary>Marks all the caller's notifications read.</summary>
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllRead(CancellationToken ct)
        {
            await notifications.MarkAllReadAsync(currentUser.RequireUserId(), ct);
            return NoContent();
        }
    }
}
