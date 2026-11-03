using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Identity.DTOs;

namespace Application.Modules.Identity.Services.Interfaces
{
    /// <summary>
    /// Email OTP and optional Google authentication. Issues a JWT access token plus a rotating refresh token; the
    /// account is created on the first successful verification.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>Validates the email, enforces rate limits, and sends an OTP.</summary>
        Task RequestOtpAsync(OtpRequestRequest request, CancellationToken ct);

        /// <summary>Verifies the OTP, creating the user on first verify, and returns a session.</summary>
        Task<AuthResponse> VerifyOtpAsync(OtpVerifyRequest request, string? ipAddress, CancellationToken ct);

        Task<AuthResponse> GoogleSignInAsync(GoogleSignInRequest request, string? ipAddress, CancellationToken ct);

        /// <summary>Rotates the refresh token and returns a fresh session.</summary>
        Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken ct);

        /// <summary>Revokes the supplied refresh token.</summary>
        Task LogoutAsync(RefreshRequest request, CancellationToken ct);

    }
}
