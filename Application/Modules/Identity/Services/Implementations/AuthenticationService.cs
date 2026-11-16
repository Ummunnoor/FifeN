using System.Security.Cryptography;
using System.Text;
using Application.Abstractions;
using Application.Exceptions;
using Application.Modules.Identity;
using Application.Modules.Identity.DTOs;
using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Modules.Identity.Services.Implementations;

public sealed class AuthenticationService(
    IEmailVerificationStore otpStore, IUserAccountStore userStore, IRefreshTokenStore refreshTokenStore,
    ITokenService tokenService, IEmailOtpSender otpSender, IGoogleIdTokenVerifier googleVerifier,
    ISecureHasher hasher, ILogger<AuthenticationService> logger, IMfaStore? mfaStore = null) : IAuthenticationService
{
    private const int MaxAttempts = 5;

    public async Task RequestOtpAsync(OtpRequestRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (!System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email)
            throw new InputValidationException(new Dictionary<string, string[]> { [nameof(request.Email)] = ["Enter a valid email address."] });
        var now = DateTimeOffset.UtcNow;
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        if (!await otpStore.TryAddWithinLimitAsync(new EmailVerification { Id = Guid.CreateVersion7(), Email = email,
            CodeHash = hasher.Hash(email + ":" + code),
            ExpiresAtUtc = now + AuthenticationTokenPolicy.OtpLifetime, CreatedAtUtc = now }, now.AddHours(-1), 5, ct))
            throw new RateLimitException("Too many codes requested. Try again after an hour.");
        await otpSender.SendAsync(email, code, ct);
        logger.LogInformation("Email OTP sent to {MaskedEmail}.", MaskEmail(email));
    }

    public async Task<AuthResponse> VerifyOtpAsync(OtpVerifyRequest request, string? ipAddress, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var challenge = await otpStore.GetLatestPendingAsync(email, ct)
            ?? throw new UnauthorizedException("Invalid email address or code.");
        if (challenge.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        { challenge.IsConsumed = true; await otpStore.UpdateAsync(challenge, ct); throw new GoneException("This code has expired. Request a new one."); }
        if (challenge.Attempts >= MaxAttempts)
            throw new LockedException("Too many incorrect attempts. Request a new code.");
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(challenge.CodeHash),
                Encoding.UTF8.GetBytes(hasher.Hash(email + ":" + request.Code))))
        {
            challenge.Attempts++;
            if (challenge.Attempts >= MaxAttempts) challenge.IsConsumed = true;
            await otpStore.UpdateAsync(challenge, ct);
            if (challenge.Attempts >= MaxAttempts)
                throw new LockedException("Too many incorrect attempts. Request a new code.");
            throw new UnauthorizedException("The code is incorrect.");
        }
        challenge.IsConsumed = true;
        await otpStore.UpdateAsync(challenge, ct);
        var user = await userStore.FindByEmailAsync(email, ct) ?? await userStore.CreateBuyerAsync(email, null, null, null, ct);
        if (user.Status != Domain.Entities.Enums.UserStatus.Active) throw new UnauthorizedException("This account is unavailable.");
        await userStore.TouchLastActiveAsync(user.Id, ct);
        return await CreateLoginResponseAsync(user, ipAddress, request.RememberDevice, ct);
    }

    public async Task<AuthResponse> GoogleSignInAsync(GoogleSignInRequest request, string? ipAddress, CancellationToken ct)
    {
        var identity = await googleVerifier.VerifyAsync(request.IdToken, ct)
            ?? throw new UnauthorizedException("Google sign-in could not be verified.");
        var email = NormalizeEmail(identity.Email);
        var user = await userStore.FindByEmailAsync(email, ct);
        user = user is null
            ? await userStore.CreateBuyerAsync(email, identity.FirstName, identity.LastName, identity.SubjectId, ct)
            : await userStore.LinkGoogleAsync(user, identity.SubjectId, identity.FirstName, identity.LastName, ct);
        if (user.Status != Domain.Entities.Enums.UserStatus.Active) throw new UnauthorizedException("This account is unavailable.");
        await userStore.TouchLastActiveAsync(user.Id, ct);
        return await CreateLoginResponseAsync(user, ipAddress, request.RememberDevice, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken ct)
    {
        var userId = await refreshTokenStore.FindUserIdAsync(request.RefreshToken, ct)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");
        var currentUser = await userStore.FindByIdAsync(userId, ct)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");
        if (currentUser.Status != Domain.Entities.Enums.UserStatus.Active)
        {
            await refreshTokenStore.RevokeAsync(request.RefreshToken, ct);
            throw new UnauthorizedException("This account is unavailable.");
        }
        var mfaSatisfied = await VerifyAdminMfaAsync(currentUser, request.TotpCode, ct);
        var rotated = await refreshTokenStore.RotateAsync(request.RefreshToken, ipAddress, ct)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");
        await userStore.TouchLastActiveAsync(currentUser.Id, ct);
        return await BuildResponseAsync(currentUser, rotated.Token, rotated.SessionStartedAtUtc, mfaSatisfied, ct);
    }

    public async Task<AuthResponse> CompleteAdminMfaEnrollmentAsync(Guid userId, string? ipAddress, CancellationToken ct)
    {
        var user = await userStore.FindByIdAsync(userId, ct)
            ?? throw new UnauthorizedException("The admin account is unavailable.");
        var roles = await userStore.GetRolesAsync(user, ct);
        if (user.Status != Domain.Entities.Enums.UserStatus.Active
            || !roles.Contains(nameof(Domain.Entities.Enums.AppRole.Admin))
            || !user.TwoFactorEnabled)
            throw new UnauthorizedException("Admin authenticator enrollment is incomplete.");

        return await IssueSessionAsync(user, ipAddress, remember: false, mfaSatisfied: true, ct);
    }

    public async Task<AuthResponse> CompleteAdminMfaChallengeAsync(
        Guid userId, string code, string? ipAddress, CancellationToken ct)
    {
        var user = await userStore.FindByIdAsync(userId, ct)
            ?? throw new UnauthorizedException("The admin account is unavailable.");
        var roles = await userStore.GetRolesAsync(user, ct);
        if (user.Status != Domain.Entities.Enums.UserStatus.Active
            || !roles.Contains(nameof(Domain.Entities.Enums.AppRole.Admin))
            || !user.TwoFactorEnabled)
            throw new UnauthorizedException("The admin MFA challenge is no longer valid.");
        if (!await IsValidAdminTotpAsync(user, code, ct))
            throw new UnauthorizedException("Invalid authenticator code.");

        await userStore.TouchLastActiveAsync(user.Id, ct);
        return await IssueSessionAsync(user, ipAddress, remember: false, mfaSatisfied: true, ct);
    }

    public Task LogoutAsync(RefreshRequest request, CancellationToken ct) => refreshTokenStore.RevokeAsync(request.RefreshToken, ct);

    private async Task<AuthResponse> IssueSessionAsync(User user, string? ipAddress, bool remember, bool mfaSatisfied, CancellationToken ct)
    {
        var issued = await refreshTokenStore.IssueAsync(user.Id, ipAddress, remember, ct);
        return await BuildResponseAsync(user, issued.Token, issued.SessionStartedAtUtc, mfaSatisfied, ct);
    }

    private async Task<AuthResponse> BuildResponseAsync(
        User user, string refresh, DateTimeOffset sessionStartedAtUtc, bool mfaSatisfied, CancellationToken ct)
    {
        var roles = await userStore.GetRolesAsync(user, ct);
        var (access, expiry) = tokenService.CreateAccessToken(
            user, roles, mfaSatisfied, sessionStartedAtUtc);
        return new AuthResponse(access, refresh, expiry,
            new UserSummary(user.Id, user.Email ?? string.Empty, user.DisplayName, user.IsVendor, user.IsAdmin));
    }

    private async Task<AuthResponse> CreateLoginResponseAsync(
        User user, string? ipAddress, bool remember, CancellationToken ct)
    {
        var roles = await userStore.GetRolesAsync(user, ct);
        if (!roles.Contains(nameof(Domain.Entities.Enums.AppRole.Admin)))
            return await IssueSessionAsync(user, ipAddress, remember, mfaSatisfied: false, ct);

        if (!user.TwoFactorEnabled)
        {
            var setupToken = tokenService.CreateMfaSetupToken(user, DateTimeOffset.UtcNow);
            return new AuthResponse(null, null, null,
                new UserSummary(user.Id, user.Email ?? string.Empty, user.DisplayName, user.IsVendor, user.IsAdmin),
                MfaEnrollmentRequired: true, MfaSetupToken: setupToken);
        }

        var challengeToken = tokenService.CreateMfaChallengeToken(user, DateTimeOffset.UtcNow);
        return new AuthResponse(null, null, null,
            new UserSummary(user.Id, user.Email ?? string.Empty, user.DisplayName, user.IsVendor, user.IsAdmin),
            MfaVerificationRequired: true, MfaChallengeToken: challengeToken);
    }

    private async Task<bool> VerifyAdminMfaAsync(User user, string? code, CancellationToken ct)
    {
        var roles = await userStore.GetRolesAsync(user, ct);
        if (!roles.Contains(nameof(Domain.Entities.Enums.AppRole.Admin)))
            return false;
        if (!user.TwoFactorEnabled || !await IsValidAdminTotpAsync(user, code, ct))
            throw new UnauthorizedException("Invalid authenticator code. Sign in with email OTP to enroll if needed.");
        return true;
    }

    private Task<bool> IsValidAdminTotpAsync(User user, string? code, CancellationToken ct) =>
        !string.IsNullOrWhiteSpace(code) && mfaStore is not null
            ? mfaStore.VerifyCodeAsync(user.Id, code.Trim(), ct)
            : Task.FromResult(false);

    private static string NormalizeEmail(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();
    private static string MaskEmail(string email) { var at = email.IndexOf('@'); return at < 1 ? "***" : email[..1] + "***" + email[at..]; }
}
