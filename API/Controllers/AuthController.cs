using API.RateLimiting;
using Application.Modules.Identity.DTOs;
using Application.Modules.Identity.Services.Interfaces;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers
{
    /// <summary>Email OTP and optional Google authentication endpoints.</summary>
    [ApiController]
    [Route("api/v1/auth")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public class AuthController(
        IAuthenticationService auth, IMfaService mfa, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>Sends a one-time passcode by email.</summary>
        [HttpPost("otp/request")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestOtp([FromBody] OtpRequestRequest request, CancellationToken ct)
        {
            await auth.RequestOtpAsync(request, ct);
            return Ok();
        }

        /// <summary>Verifies the passcode and returns a session, creating the account on first verify.</summary>
        [HttpPost("otp/verify")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> VerifyOtp([FromBody] OtpVerifyRequest request, CancellationToken ct)
        {
            var response = await auth.VerifyOtpAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return Ok(response);
        }

        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Google([FromBody] GoogleSignInRequest request, CancellationToken ct) =>
            Ok(await auth.GoogleSignInAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct));

        /// <summary>Rotates the refresh token and returns a fresh session.</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
        {
            var response = await auth.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return Ok(response);
        }

        /// <summary>Revokes the supplied refresh token.</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
        {
            await auth.LogoutAsync(request, ct);
            return NoContent();
        }


        /// <summary>Begins TOTP enrollment for an admin; returns the otpauth URI to scan. Authorizes on
        /// the Admin role alone (not the MFA-gated <c>RequireAdmin</c> policy) so MFA can be bootstrapped.</summary>
        [HttpPost("mfa/enroll")]
        [Authorize(Policy = "RequireMfaSetup")]
        public async Task<ActionResult<MfaEnrollResponse>> EnrollMfa(CancellationToken ct) =>
            Ok(await mfa.EnrollAsync(currentUser.RequireUserId(), ct));

        /// <summary>Activates MFA after confirming a code and issues the admin session.</summary>
        [HttpPost("mfa/verify")]
        [Authorize(Policy = "RequireMfaSetup")]
        public async Task<ActionResult<AuthResponse>> VerifyMfa([FromBody] MfaVerifyRequest request, CancellationToken ct)
        {
            await mfa.VerifyAsync(currentUser.RequireUserId(), request, ct);
            return Ok(await auth.CompleteAdminMfaEnrollmentAsync(
                currentUser.RequireUserId(), currentUser.IpAddress, ct));
        }

        /// <summary>Completes the second factor for an already-enrolled admin sign-in.</summary>
        [HttpPost("mfa/login/verify")]
        [Authorize(Policy = "RequireMfaChallenge")]
        public async Task<ActionResult<AuthResponse>> VerifyLoginMfa(
            [FromBody] MfaVerifyRequest request, CancellationToken ct) =>
            Ok(await auth.CompleteAdminMfaChallengeAsync(
                currentUser.RequireUserId(), request.Code, currentUser.IpAddress, ct));
    }
}
