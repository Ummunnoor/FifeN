namespace Application.Modules.Identity.DTOs;

public record OtpRequestRequest(string Email);
public record OtpVerifyRequest(string Email, string Code, bool RememberDevice = false, string? TotpCode = null);
public record GoogleSignInRequest(string IdToken, bool RememberDevice = false);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc, UserSummary User);
public record UserSummary(Guid Id, string Email, string DisplayName, bool IsVendor, bool IsAdmin);
public record MfaEnrollResponse(string OtpAuthUri);
public record MfaVerifyRequest(string Code);
