using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Enums;
using Domain.Entities.Identity;

namespace Application.Modules.Identity.Services.Interfaces
{
    /// <summary>Issues signed JWT access tokens. Implemented in the persistence/infrastructure layer.</summary>
    public interface ITokenService
    {
        /// <summary>Builds a signed access token for the user and returns it with its expiry.</summary>
        (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(
            User user, IReadOnlyCollection<string> roles, bool mfaSatisfied, DateTimeOffset sessionStartedAtUtc);
    }

    /// <summary>Persists hashed, rotating refresh tokens.</summary>
    public interface IRefreshTokenStore
    {
        /// <summary>Starts a session and returns its refresh token and original UTC start time.</summary>
        Task<(string Token, DateTimeOffset SessionStartedAtUtc)> IssueAsync(
            Guid userId, string? ipAddress, bool rememberDevice, CancellationToken ct);

        /// <summary>
        /// Revokes a still-active token and issues a replacement while preserving the original session
        /// start. Returns null if the token is missing, expired, revoked, or beyond the absolute session limit.
        /// </summary>
        Task<(Guid UserId, string Token, DateTimeOffset SessionStartedAtUtc)?> RotateAsync(
            string oldRawToken, string? ipAddress, CancellationToken ct);

        /// <summary>Revokes the supplied raw token if it exists.</summary>
        Task RevokeAsync(string rawToken, CancellationToken ct);
    }

    /// <summary>Account lookup/creation backed by ASP.NET Identity.</summary>
    public interface IUserAccountStore
    {
        Task<User?> FindByEmailAsync(string email, CancellationToken ct);

        /// <summary>Creates a buyer account (User role) for a freshly verified phone number.</summary>
        Task<User> CreateBuyerAsync(string email, string? firstName, string? lastName, string? googleSubjectId, CancellationToken ct);

        Task<User> LinkGoogleAsync(User user, string subjectId, string? firstName, string? lastName, CancellationToken ct);

        Task<IReadOnlyCollection<string>> GetRolesAsync(User user, CancellationToken ct);

        Task TouchLastActiveAsync(Guid userId, CancellationToken ct);

        Task<User?> FindByIdAsync(Guid userId, CancellationToken ct);

        /// <summary>True if the phone is any user's primary or secondary number, ignoring the given user.</summary>
    }

    /// <summary>Stores and queries transient OTP challenges.</summary>
    public interface IEmailVerificationStore
    {
        /// <summary>Counts OTP requests for a phone number since the given instant (rate limiting).</summary>
        Task<int> CountRequestsSinceAsync(string email, DateTimeOffset since, CancellationToken ct);

        Task AddAsync(Domain.Entities.Identity.EmailVerification verification, CancellationToken ct);

        /// <summary>Returns the most recent pending challenge for a phone number, if any.</summary>
        Task<Domain.Entities.Identity.EmailVerification?> GetLatestPendingAsync(string email, CancellationToken ct);

        Task UpdateAsync(Domain.Entities.Identity.EmailVerification verification, CancellationToken ct);
    }

    /// <summary>TOTP (authenticator-app) enrollment and verification, backed by ASP.NET Identity.</summary>
    public interface IMfaStore
    {
        /// <summary>(Re)generates the user's authenticator key and returns the <c>otpauth://</c> URI to scan.</summary>
        Task<string> ResetAndBuildAuthenticatorUriAsync(Guid userId, CancellationToken ct);

        /// <summary>Verifies a TOTP code and, on success, enables two-factor for the user.</summary>
        Task<bool> VerifyAndEnableAsync(Guid userId, string code, CancellationToken ct);

        /// <summary>Verifies a TOTP code against the user's existing authenticator key (does not change state).</summary>
        Task<bool> VerifyCodeAsync(Guid userId, string code, CancellationToken ct);
    }

    public interface IEmailOtpSender
    {
        /// <summary>Sends the code and returns the channel it was delivered over.</summary>
        Task SendAsync(string email, string code, CancellationToken ct);
    }

    public interface IGoogleIdTokenVerifier
    {
        Task<GoogleUserIdentity?> VerifyAsync(string token, CancellationToken ct);
    }

    public sealed record GoogleUserIdentity(string SubjectId, string Email, string? FirstName, string? LastName);
}
