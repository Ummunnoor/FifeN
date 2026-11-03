using Application.Modules.Identity;
using System.Globalization;
using Xunit;

namespace FifeN.Tests.Unit;

public sealed class AuthenticationTokenPolicyTests
{
    [Fact]
    public void IsSessionWithinAbsoluteLifetime_RejectsAtAndAfterThirtyDays()
    {
        var sessionStartedAtUtc = DateTimeOffset.UtcNow.AddDays(-30);

        Assert.False(AuthenticationTokenPolicy.IsSessionWithinAbsoluteLifetime(
            sessionStartedAtUtc, sessionStartedAtUtc + AuthenticationTokenPolicy.AbsoluteSessionLifetime));
        Assert.False(AuthenticationTokenPolicy.IsSessionWithinAbsoluteLifetime(
            sessionStartedAtUtc,
            sessionStartedAtUtc + AuthenticationTokenPolicy.AbsoluteSessionLifetime + TimeSpan.FromTicks(1)));
    }

    [Fact]
    public void IsSessionWithinAbsoluteLifetime_RejectsFutureStart()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.False(AuthenticationTokenPolicy.IsSessionWithinAbsoluteLifetime(
            now.AddTicks(1), now));
    }

    [Fact]
    public void TryParseSessionStartedAtClaim_ParsesIssuedClaimAndRejectsInvalidValues()
    {
        var sessionStartedAtUtc = DateTimeOffset.UtcNow;
        var value = sessionStartedAtUtc.ToString("O", CultureInfo.InvariantCulture);

        Assert.True(AuthenticationTokenPolicy.TryParseSessionStartedAtClaim(value, out var parsed));
        Assert.Equal(sessionStartedAtUtc, parsed);
        Assert.False(AuthenticationTokenPolicy.TryParseSessionStartedAtClaim("invalid", out _));
    }
}
