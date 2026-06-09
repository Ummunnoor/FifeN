using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Application.Services.Interfaces
{
    /// <summary>Exposes the authenticated caller's identity to the application layer.</summary>
    public interface ICurrentUserService
    {
        /// <summary>The caller's user id, or null if anonymous.</summary>
        Guid? UserId { get; }

        /// <summary>True when the request carries a valid authenticated principal.</summary>
        bool IsAuthenticated { get; }

        /// <summary>True when the caller holds the Admin role.</summary>
        bool IsAdmin { get; }

        /// <summary>True when the caller holds the Vendor role.</summary>
        bool IsVendor { get; }

        /// <summary>True when the caller is the platform owner (founder).</summary>
        bool IsOwner { get; }

        /// <summary>Originating IP address of the current request, if known.</summary>
        string? IpAddress { get; }

        /// <summary>All claims on the current principal.</summary>
        IEnumerable<Claim> Claims { get; }

        /// <summary>The caller's user id, throwing <see cref="System.UnauthorizedAccessException"/> if anonymous.</summary>
        Guid RequireUserId();
    }
}
