using API.Hubs;
using Application.DTOs;
using Application.Modules.Engagement.DTOs;
using Application.Modules.Engagement.Services.Interfaces;
using Application.Services.Interfaces;
using Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Controllers
{
    /// <summary>Buyer lead capture, in-app chat, and the vendor leads dashboard.</summary>
    [ApiController]
    [Route("api/v1")]
    public class InteractionController(
        IInteractionService interactions,
        ICurrentUserService currentUser,
        IHubContext<ChatHub> chatHub) : ControllerBase
    {
        /// <summary>Records a lead, then returns the in-app chat path for buyer-vendor conversation.</summary>
        [HttpPost("products/{id:guid}/interest")]
        [Authorize]
        public async Task<ActionResult<InterestResponse>> ExpressInterest(
            Guid id, [FromBody] ExpressInterestRequest request, CancellationToken ct)
        {
            var response = await interactions.ExpressInterestAsync(currentUser.RequireUserId(), id, request, ct);
            if (response.Message is { } message)
                await chatHub.Clients.Group(ChatHub.GroupName(response.InteractionId))
                    .SendAsync("MessageReceived", message, ct);

            return StatusCode(StatusCodes.Status201Created, response);
        }

        /// <summary>Lists the caller's conversations, newest first.</summary>
        [HttpGet("chats")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<ChatSummaryResponse>>> MyChats(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
            Ok(await interactions.GetChatsAsync(currentUser.RequireUserId(), page, pageSize, ct));

        /// <summary>Reads messages for a conversation the caller participates in.</summary>
        [HttpGet("chats/{id:guid}/messages")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<ChatMessageResponse>>> ChatMessages(
            Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
            Ok(await interactions.GetChatMessagesAsync(currentUser.RequireUserId(), id, page, pageSize, ct));

        /// <summary>Sends a message in a conversation the caller participates in.</summary>
        [HttpPost("chats/{id:guid}/messages")]
        [Authorize]
        public async Task<ActionResult<ChatMessageResponse>> SendChatMessage(
            Guid id, [FromBody] SendChatMessageRequest request, CancellationToken ct)
        {
            var message = await interactions.SendChatMessageAsync(currentUser.RequireUserId(), id, request, ct);
            await chatHub.Clients.Group(ChatHub.GroupName(id)).SendAsync("MessageReceived", message, ct);
            return StatusCode(StatusCodes.Status201Created, message);
        }

        /// <summary>Adds automated payment instructions to the chat.</summary>
        [HttpPost("chats/{id:guid}/proceed-to-pay")]
        [Authorize]
        public async Task<ActionResult<PaymentInstructionsResponse>> ProceedToPay(Guid id, CancellationToken ct)
        {
            var response = await interactions.ProceedToPayAsync(currentUser.RequireUserId(), id, ct);
            await chatHub.Clients.Group(ChatHub.GroupName(id)).SendAsync("MessageReceived", response.Message, ct);
            return Ok(response);
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
