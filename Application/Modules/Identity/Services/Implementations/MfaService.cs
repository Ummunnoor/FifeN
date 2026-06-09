using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Modules.Identity.DTOs;
using Application.Modules.Identity.Services.Interfaces;

namespace Application.Modules.Identity.Services.Implementations
{
    /// <summary>
    /// Orchestrates admin TOTP enrollment. The cryptographic work (key generation, code verification,
    /// enabling two-factor) lives behind <see cref="IMfaStore"/>; this service maps it to DTOs and the
    /// API's error contract.
    /// </summary>
    public sealed class MfaService(IMfaStore mfa) : IMfaService
    {
        public async Task<MfaEnrollResponse> EnrollAsync(Guid userId, CancellationToken ct)
        {
            var uri = await mfa.ResetAndBuildAuthenticatorUriAsync(userId, ct);
            return new MfaEnrollResponse(uri);
        }

        public async Task VerifyAsync(Guid userId, MfaVerifyRequest request, CancellationToken ct)
        {
            if (!await mfa.VerifyAndEnableAsync(userId, request.Code?.Trim() ?? string.Empty, ct))
                throw new UnauthorizedException("Invalid authentication code.");
        }
    }
}
