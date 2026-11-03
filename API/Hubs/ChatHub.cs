using Application.Modules.Engagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace API.Hubs
{
    /// <summary>SignalR hub for live buyer-vendor conversation updates.</summary>
    [Authorize]
    public sealed class ChatHub(IInteractionService interactions) : Hub
    {
        public static string GroupName(Guid interactionId) => $"chat:{interactionId}";

        public async Task JoinConversation(Guid interactionId)
        {
            await interactions.EnsureConversationAccessAsync(
                RequireUserId(), interactionId, Context.ConnectionAborted);
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(interactionId), Context.ConnectionAborted);
        }

        public Task LeaveConversation(Guid interactionId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(interactionId), Context.ConnectionAborted);

        private Guid RequireUserId() =>
            Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : throw new HubException("No authenticated user on the current connection.");
    }
}
