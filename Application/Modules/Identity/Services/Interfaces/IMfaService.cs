using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Identity.DTOs;

namespace Application.Modules.Identity.Services.Interfaces
{
    /// <summary>
    /// Admin TOTP multi-factor enrollment (spec §3.1 / §4.1). Enrollment issues an authenticator
    /// secret; verification activates MFA so the account can carry the <c>amr=mfa</c> capability.
    /// </summary>
    public interface IMfaService
    {
        /// <summary>Starts (or restarts) TOTP enrollment for the caller; returns the URI to scan.</summary>
        Task<MfaEnrollResponse> EnrollAsync(Guid userId, CancellationToken ct);

        /// <summary>Activates MFA after confirming a code from the authenticator app.</summary>
        Task VerifyAsync(Guid userId, MfaVerifyRequest request, CancellationToken ct);
    }
}
