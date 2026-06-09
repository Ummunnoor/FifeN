using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.Engagement.DTOs;
using Application.Modules.Engagement.Services.Interfaces;
using Application.Services.Interfaces;
using Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Buyer lead capture (the WhatsApp hand-off) and the vendor leads dashboard.</summary>
    [ApiController]
    [Route("api/v1")]
    public class InteractionController(IInteractionService interactions, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>Records a lead, then returns a pre-filled WhatsApp link to the vendor.</summary>
        [HttpPost("products/{id:guid}/interest")]
        [Authorize]
        public async Task<ActionResult<InterestResponse>> ExpressInterest(
            Guid id, [FromBody] ExpressInterestRequest request, CancellationToken ct)
        {
            var response = await interactions.ExpressInterestAsync(currentUser.RequireUserId(), id, request, ct);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        /// <summary>The caller's leads, optionally filtered by status.</summary>
        [HttpGet("vendor/leads")]
        [Authorize(Policy = "RequireVendor")]
        public async Task<ActionResult<PagedResponse<LeadResponse>>> MyLeads(
            [FromQuery] LeadStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
            Ok(await interactions.GetVendorLeadsAsync(currentUser.RequireUserId(), status, page, pageSize, ct));

        /// <summary>Marks a lead Viewed or Closed (owner only).</summary>
        [HttpPatch("vendor/leads/{id:guid}")]
        [Authorize(Policy = "RequireVendor")]
        public async Task<IActionResult> UpdateLead(
            Guid id, [FromBody] UpdateLeadRequest request, CancellationToken ct)
        {
            await interactions.UpdateLeadAsync(currentUser.RequireUserId(), id, request, ct);
            return Ok();
        }
    }
}
