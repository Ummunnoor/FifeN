namespace Application.Modules.Identity.DTOs;

public record OtpRequestRequest(string Email);
public record OtpVerifyRequest(string Email, string Code, bool RememberDevice = false);
public record GoogleSignInRequest(string IdToken, bool RememberDevice = false);
public record RefreshRequest(string RefreshToken, string? TotpCode = null);
public record AuthResponse(
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAtUtc,
    UserSummary User,
    bool MfaEnrollmentRequired = false,
    string? MfaSetupToken = null,
    bool MfaVerificationRequired = false,
    string? MfaChallengeToken = null);
public record UserSummary(Guid Id, string Email, string DisplayName, bool IsVendor, bool IsAdmin);
public record MfaEnrollResponse(string OtpAuthUri);
public record MfaVerifyRequest(string Code);
