using System;
using System.Collections.Generic;
using System.Security.Claims;
using Application.Services.Interfaces;
using Domain.Entities.Enums;
using Microsoft.AspNetCore.Http;

namespace Persistence.Services
{
    /// <summary>Reads the authenticated caller's identity from the current HTTP request.</summary>
    public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

        public Guid? UserId =>
            Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

        public bool IsAdmin => Principal?.IsInRole(nameof(AppRole.Admin)) ?? false;

        public bool IsVendor => Principal?.IsInRole(nameof(AppRole.Vendor)) ?? false;

        public bool IsOwner => Principal?.FindFirstValue("is_owner") == "true";

        public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        public IEnumerable<Claim> Claims => Principal?.Claims ?? [];

        public Guid RequireUserId() =>
            UserId ?? throw new UnauthorizedAccessException("No authenticated user on the current request.");
    }
}
