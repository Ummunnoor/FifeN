using System;

namespace Application.Modules.Identity.DTOs
{
    /// <summary>Request to send a one-time passcode to a Nigerian phone number.</summary>
    public record OtpRequestRequest(string PhoneNumber);

    /// <summary>
    /// Request to verify a one-time passcode. Creates the account on first successful verify.
    /// MFA-enrolled admins must also supply <see cref="TotpCode"/> (their authenticator code) — the
    /// OTP alone never satisfies the admin second-factor gate.
    /// </summary>
    public record OtpVerifyRequest(string PhoneNumber, string Code, string? TotpCode = null);

    /// <summary>Request to exchange a refresh token for a fresh access/refresh pair.</summary>
    public record RefreshRequest(string RefreshToken);

    /// <summary>The authenticated session returned after verify/refresh.</summary>
    public record AuthResponse(
        string AccessToken,
        string RefreshToken,
        DateTimeOffset ExpiresAtUtc,
        UserSummary User);

    /// <summary>Lightweight identity projection embedded in <see cref="AuthResponse"/>.</summary>
    public record UserSummary(Guid Id, string DisplayName, bool IsVendor, bool IsAdmin);

    /// <summary>Request to send an OTP to a proposed recovery (secondary) phone number.</summary>
    public record AddSecondaryPhoneRequest(string PhoneNumber);

    /// <summary>Request to confirm and bind a recovery phone number with its OTP.</summary>
    public record VerifySecondaryPhoneRequest(string PhoneNumber, string Code);

    /// <summary>TOTP enrollment result: the <c>otpauth://</c> URI an authenticator app scans.</summary>
    public record MfaEnrollResponse(string OtpAuthUri);

    /// <summary>The 6-digit code from the authenticator app, presented to activate MFA.</summary>
    public record MfaVerifyRequest(string Code);
}
