using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Exceptions;
using Application.Modules.Identity.DTOs;
using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Modules.Identity.Services.Implementations
{
    /// <summary>
    /// Phone + OTP authentication. The account is created on first successful verification; sessions
    /// are a short-lived JWT plus a rotating, hashed refresh token.
    /// </summary>
    public class AuthenticationService(
        IPhoneVerificationStore otpStore,
        IUserAccountStore userStore,
        IRefreshTokenStore refreshTokenStore,
        ITokenService tokenService,
        IOtpSender otpSender,
        ISecureHasher hasher,
        IMfaStore mfaStore,
        ILogger<AuthenticationService> logger) : IAuthenticationService
    {
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
        private const int MaxAttempts = 3;

        // Per-phone OTP request rate limits (BRD §5.1).
        private static readonly (TimeSpan Window, int Max)[] RequestLimits =
        [
            (TimeSpan.FromMinutes(15), 3),
            (TimeSpan.FromHours(1), 5),
            (TimeSpan.FromDays(1), 10)
        ];

        public async Task RequestOtpAsync(OtpRequestRequest request, CancellationToken ct)
        {
            if (!NigerianPhoneNumber.TryNormalize(request.PhoneNumber, out var phone))
                throw new InputValidationException(new Dictionary<string, string[]>
                {
                    [nameof(request.PhoneNumber)] = ["A valid Nigerian (+234) mobile number is required."]
                });

            await IssueOtpAsync(phone, ct);
        }

        /// <summary>Enforces per-phone request limits, then generates, sends, and stores an OTP challenge.</summary>
        private async Task IssueOtpAsync(string phone, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var (window, max) in RequestLimits)
            {
                var count = await otpStore.CountRequestsSinceAsync(phone, now - window, ct);
                if (count >= max)
                    throw new RateLimitException(
                        $"Too many codes requested. Try again later (limit {max} per {Describe(window)}).");
            }

            var code = GenerateCode();
            var verification = new PhoneVerification
            {
                Id = Guid.CreateVersion7(),
                PhoneNumber = phone,
                CodeHash = hasher.Hash(code),
                ExpiresAtUtc = now + OtpLifetime,
                Status = OtpStatus.Pending,
                CreatedAtUtc = now
            };

            var channel = await otpSender.SendAsync(phone, code, ct);
            verification.Channel = channel;

            await otpStore.AddAsync(verification, ct);
            logger.LogInformation("OTP issued for {Phone} via {Channel}.", Mask(phone), channel);
        }

        /// <summary>
        /// Validates the latest pending OTP for a phone and marks it verified. Throws on a missing,
        /// locked, expired, or incorrect code (incrementing attempts and locking after three failures).
        /// </summary>
        private async Task ConsumeChallengeAsync(string phone, string code, CancellationToken ct)
        {
            var challenge = await otpStore.GetLatestPendingAsync(phone, ct)
                ?? throw new UnauthorizedException("Invalid phone number or code.");

            if (challenge.Status == OtpStatus.Locked)
                throw new LockedException("This code is locked after too many attempts. Request a new one.");

            if (challenge.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                challenge.Status = OtpStatus.Expired;
                await otpStore.UpdateAsync(challenge, ct);
                throw new GoneException("This code has expired. Request a new one.");
            }

            if (!FixedTimeEquals(challenge.CodeHash, hasher.Hash(code)))
            {
                challenge.Attempts++;
                if (challenge.Attempts >= MaxAttempts)
                    challenge.Status = OtpStatus.Locked;
                await otpStore.UpdateAsync(challenge, ct);
                throw new UnauthorizedException("Invalid phone number or code.");
            }

            challenge.Status = OtpStatus.Verified;
            await otpStore.UpdateAsync(challenge, ct);
        }

        public async Task<AuthResponse> VerifyOtpAsync(OtpVerifyRequest request, string? ipAddress, CancellationToken ct)
        {
            if (!NigerianPhoneNumber.TryNormalize(request.PhoneNumber, out var phone))
                throw new UnauthorizedException("Invalid phone number or code.");

            // Resolve the account first (without consuming the OTP) to decide whether a second factor is
            // required. MFA-enrolled admins must present a live authenticator code as well as the OTP —
            // the OTP alone must never mint the amr=mfa capability that gates RequireAdmin.
            var existing = await userStore.FindByPhoneAsync(phone, ct);
            var mfaRequired = existing is { IsAdmin: true, TwoFactorEnabled: true };

            // Fail before consuming the OTP so the challenge stays valid for the resubmit.
            if (mfaRequired && string.IsNullOrWhiteSpace(request.TotpCode))
                throw new MfaRequiredException(
                    "An authenticator code is required. Resubmit with both the OTP and your authenticator code.");

            // First factor: the SMS/WhatsApp OTP (consumes the challenge).
            await ConsumeChallengeAsync(phone, request.Code, ct);

            // Second factor: the TOTP authenticator code, for MFA-enrolled admins.
            if (mfaRequired && !await mfaStore.VerifyCodeAsync(existing!.Id, request.TotpCode!.Trim(), ct))
                throw new UnauthorizedException("Invalid authentication code.");

            // A brand-new account (existing is null) is always a buyer, never an admin, so no MFA path.
            var user = existing ?? await userStore.CreateBuyerAsync(phone, ct);

            await userStore.TouchLastActiveAsync(user.Id, ct);
            return await IssueSessionAsync(user, ipAddress, ct);
        }

        public async Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken ct)
        {
            var rotated = await refreshTokenStore.RotateAsync(request.RefreshToken, ipAddress, ct)
                ?? throw new UnauthorizedException("Invalid or expired refresh token.");

            var user = await userStore.FindByIdAsync(rotated.UserId, ct)
                ?? throw new UnauthorizedException("Invalid or expired refresh token.");

            await userStore.TouchLastActiveAsync(user.Id, ct);
            return await BuildResponseAsync(user, rotated.Token, ct);
        }

        public Task LogoutAsync(RefreshRequest request, CancellationToken ct) =>
            refreshTokenStore.RevokeAsync(request.RefreshToken, ct);

        public async Task RequestSecondaryPhoneAsync(
            Guid userId, AddSecondaryPhoneRequest request, CancellationToken ct)
        {
            if (!NigerianPhoneNumber.TryNormalize(request.PhoneNumber, out var phone))
                throw new InputValidationException(new Dictionary<string, string[]>
                {
                    [nameof(request.PhoneNumber)] = ["A valid Nigerian (+234) mobile number is required."]
                });

            if (await userStore.PhoneInUseAsync(phone, userId, ct))
                throw new ConflictException("That phone number is already in use.");

            await IssueOtpAsync(phone, ct);
        }

        public async Task VerifySecondaryPhoneAsync(
            Guid userId, VerifySecondaryPhoneRequest request, CancellationToken ct)
        {
            if (!NigerianPhoneNumber.TryNormalize(request.PhoneNumber, out var phone))
                throw new UnauthorizedException("Invalid phone number or code.");

            await ConsumeChallengeAsync(phone, request.Code, ct);

            // Re-check after verification to close the race where the number was claimed meanwhile.
            if (await userStore.PhoneInUseAsync(phone, userId, ct))
                throw new ConflictException("That phone number is already in use.");

            await userStore.SetSecondaryPhoneAsync(userId, phone, ct);
            await userStore.TouchLastActiveAsync(userId, ct);
        }

        private async Task<AuthResponse> IssueSessionAsync(User user, string? ipAddress, CancellationToken ct)
        {
            var refresh = await refreshTokenStore.IssueAsync(user.Id, ipAddress, ct);
            return await BuildResponseAsync(user, refresh, ct);
        }

        private async Task<AuthResponse> BuildResponseAsync(User user, string refreshToken, CancellationToken ct)
        {
            var roles = await userStore.GetRolesAsync(user, ct);
            // An admin who has activated TOTP carries the amr=mfa capability (RequireAdmin gate).
            // There is no separate per-login TOTP challenge endpoint in the spec, so enrollment status
            // is the MFA signal; the claim refreshes whenever a new token is minted.
            var (access, expiresAtUtc) = tokenService.CreateAccessToken(user, roles, mfaSatisfied: user.TwoFactorEnabled);
            var summary = new UserSummary(user.Id, user.DisplayName, user.IsVendor, user.IsAdmin);
            return new AuthResponse(access, refreshToken, expiresAtUtc, summary);
        }

        private static string GenerateCode() =>
            RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        private static bool FixedTimeEquals(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(a),
                System.Text.Encoding.UTF8.GetBytes(b));

        // Masks all but the last four digits so phone numbers (PII) are not retained in full in logs.
        private static string Mask(string phone) =>
            phone.Length <= 4 ? "****" : $"***{phone[^4..]}";

        private static string Describe(TimeSpan window) =>
            window.TotalDays >= 1 ? "day" : window.TotalHours >= 1 ? "hour" : $"{window.TotalMinutes:0} minutes";
    }
}
