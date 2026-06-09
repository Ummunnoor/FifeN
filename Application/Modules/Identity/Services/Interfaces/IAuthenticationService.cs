using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Identity.DTOs;

namespace Application.Modules.Identity.Services.Interfaces
{
    /// <summary>
    /// Phone + OTP authentication. Issues a JWT access token plus a rotating refresh token; the
    /// account is created on the first successful verification.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>Validates the phone number, enforces rate limits, and sends an OTP (WhatsApp-first).</summary>
        Task RequestOtpAsync(OtpRequestRequest request, CancellationToken ct);

        /// <summary>Verifies the OTP, creating the user on first verify, and returns a session.</summary>
        Task<AuthResponse> VerifyOtpAsync(OtpVerifyRequest request, string? ipAddress, CancellationToken ct);

        /// <summary>Rotates the refresh token and returns a fresh session.</summary>
        Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken ct);

        /// <summary>Revokes the supplied refresh token.</summary>
        Task LogoutAsync(RefreshRequest request, CancellationToken ct);

        /// <summary>Sends an OTP to a proposed recovery number after checking it is not already in use.</summary>
        Task RequestSecondaryPhoneAsync(Guid userId, AddSecondaryPhoneRequest request, CancellationToken ct);

        /// <summary>Verifies the OTP and binds the recovery number to the caller's account.</summary>
        Task VerifySecondaryPhoneAsync(Guid userId, VerifySecondaryPhoneRequest request, CancellationToken ct);
    }
}
