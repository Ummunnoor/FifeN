using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.Engagement.DTOs;
using Application.Modules.Engagement.Services.Interfaces;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Interaction-gated buyer reviews and public review listing.</summary>
    [ApiController]
    [Route("api/v1")]
    public class ReviewController(IReviewService reviews, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>Posts a review (eligible buyers only — must have contacted the vendor ≥48h ago).</summary>
        [HttpPost("products/{id:guid}/reviews")]
        [Authorize]
        public async Task<ActionResult<ReviewResponse>> Create(
            Guid id, [FromBody] CreateReviewRequest request, CancellationToken ct)
        {
            var review = await reviews.CreateAsync(currentUser.RequireUserId(), id, request, ct);
            return StatusCode(StatusCodes.Status201Created, review);
        }

        /// <summary>Edits the caller's review (author only, within 30 days).</summary>
        [HttpPut("reviews/{id:guid}")]
        [Authorize]
        public async Task<ActionResult<ReviewResponse>> Update(
            Guid id, [FromBody] UpdateReviewRequest request, CancellationToken ct) =>
            Ok(await reviews.UpdateAsync(currentUser.RequireUserId(), id, request, ct));

        /// <summary>A listing's visible reviews.</summary>
        [HttpGet("products/{id:guid}/reviews")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResponse<ReviewResponse>>> ForProduct(
            Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
            Ok(await reviews.GetForProductAsync(id, page, pageSize, ct));
    }
}
