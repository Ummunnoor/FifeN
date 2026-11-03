using System.Globalization;

namespace Application.Modules.Identity;

public static class AuthenticationTokenPolicy
{
    public const int AccessTokenLifetimeMinutes = 30;
    public static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan StandardRefreshTokenLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan RememberedRefreshTokenLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan AbsoluteSessionLifetime = TimeSpan.FromDays(30);

    public static bool IsSessionWithinAbsoluteLifetime(
        DateTimeOffset sessionStartedAtUtc, DateTimeOffset nowUtc) =>
        sessionStartedAtUtc <= nowUtc
        && nowUtc < sessionStartedAtUtc + AbsoluteSessionLifetime;

    public static bool TryParseSessionStartedAtClaim(string? value, out DateTimeOffset sessionStartedAtUtc) =>
        DateTimeOffset.TryParseExact(
            value, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out sessionStartedAtUtc);
}
