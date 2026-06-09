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
            User user, IReadOnlyCollection<string> roles, bool mfaSatisfied);
    }

    /// <summary>Persists hashed, rotating refresh tokens.</summary>
    public interface IRefreshTokenStore
    {
        /// <summary>Issues a new refresh token for the user and returns the raw (unhashed) value.</summary>
        Task<string> IssueAsync(Guid userId, string? ipAddress, CancellationToken ct);

        /// <summary>
        /// Revokes a still-active token and issues a replacement in one pass, returning the owning user id
        /// and the new raw token. Returns null if the token is missing, expired, or already revoked.
        /// </summary>
        Task<(Guid UserId, string Token)?> RotateAsync(string oldRawToken, string? ipAddress, CancellationToken ct);

        /// <summary>Revokes the supplied raw token if it exists.</summary>
        Task RevokeAsync(string rawToken, CancellationToken ct);
    }

    /// <summary>Account lookup/creation backed by ASP.NET Identity.</summary>
    public interface IUserAccountStore
    {
        Task<User?> FindByPhoneAsync(string phoneNumber, CancellationToken ct);

        /// <summary>Creates a buyer account (User role) for a freshly verified phone number.</summary>
        Task<User> CreateBuyerAsync(string phoneNumber, CancellationToken ct);

        Task<IReadOnlyCollection<string>> GetRolesAsync(User user, CancellationToken ct);

        Task TouchLastActiveAsync(Guid userId, CancellationToken ct);

        Task<User?> FindByIdAsync(Guid userId, CancellationToken ct);

        /// <summary>True if the phone is any user's primary or secondary number, ignoring the given user.</summary>
        Task<bool> PhoneInUseAsync(string phoneNumber, Guid excludingUserId, CancellationToken ct);

        /// <summary>Binds a verified recovery number to the user.</summary>
        Task SetSecondaryPhoneAsync(Guid userId, string phoneNumber, CancellationToken ct);
    }

    /// <summary>Stores and queries transient OTP challenges.</summary>
    public interface IPhoneVerificationStore
    {
        /// <summary>Counts OTP requests for a phone number since the given instant (rate limiting).</summary>
        Task<int> CountRequestsSinceAsync(string phoneNumber, DateTimeOffset since, CancellationToken ct);

        Task AddAsync(PhoneVerification verification, CancellationToken ct);

        /// <summary>Returns the most recent pending challenge for a phone number, if any.</summary>
        Task<PhoneVerification?> GetLatestPendingAsync(string phoneNumber, CancellationToken ct);

        Task UpdateAsync(PhoneVerification verification, CancellationToken ct);
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

    /// <summary>Delivers OTP codes over WhatsApp (preferred) or SMS via the messaging provider.</summary>
    public interface IOtpSender
    {
        /// <summary>Sends the code and returns the channel it was delivered over.</summary>
        Task<OtpChannel> SendAsync(string phoneNumber, string code, CancellationToken ct);
    }
}
