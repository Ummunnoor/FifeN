using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using Application.Modules.Identity;
using Microsoft.Extensions.Configuration;
using Persistence.Modules.Identity;
using Xunit;
using Domain.Entities.Identity;

namespace FifeN.Tests.Unit;

public sealed class TokenServiceTests
{
    [Fact]
    public void CreateAccessToken_UsesThirtyMinuteLifetime()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-that-is-at-least-thirty-two-bytes",
                ["Jwt:Issuer"] = "FifeN.Tests",
                ["Jwt:Audience"] = "FifeN.Tests",
                ["Jwt:ExpiryMinutes"] = "30"
            })
            .Build();
        var service = new TokenService(configuration);
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User"
        };
        var before = DateTimeOffset.UtcNow;
        var sessionStartedAtUtc = before;

        var (token, expiresAtUtc) = service.CreateAccessToken(
            user, Array.Empty<string>(), false, sessionStartedAtUtc);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.InRange((expiresAtUtc - before).TotalMinutes, 29.99, 30.01);
        Assert.InRange(Math.Abs((expiresAtUtc.UtcDateTime - jwt.ValidTo).TotalSeconds), 0, 1);
        Assert.Equal(
            sessionStartedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            jwt.Claims.Single(c => c.Type == "session_started_at").Value);
    }

    [Fact]
    public void CreateAccessToken_CapsExpiryAtAbsoluteSessionDeadline()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-that-is-at-least-thirty-two-bytes",
                ["Jwt:Issuer"] = "FifeN.Tests",
                ["Jwt:Audience"] = "FifeN.Tests",
                ["Jwt:ExpiryMinutes"] = "30"
            })
            .Build();
        var service = new TokenService(configuration);
        var user = new User { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User" };
        var sessionStartedAtUtc = DateTimeOffset.UtcNow.AddDays(-30).AddMinutes(5);

        var (_, expiresAtUtc) = service.CreateAccessToken(
            user, Array.Empty<string>(), false, sessionStartedAtUtc);

        Assert.Equal(sessionStartedAtUtc + AuthenticationTokenPolicy.AbsoluteSessionLifetime, expiresAtUtc);
        Assert.True(expiresAtUtc - DateTimeOffset.UtcNow < TimeSpan.FromMinutes(5));
    }

    [Theory]
    [InlineData("15")]
    [InlineData("not-a-number")]
    public void CreateAccessToken_RejectsConfigurationOutsideApprovedLifetime(string configuredMinutes)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-that-is-at-least-thirty-two-bytes",
                ["Jwt:ExpiryMinutes"] = configuredMinutes
            })
            .Build();
        var service = new TokenService(configuration);
        var user = new User { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User" };

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateAccessToken(user, Array.Empty<string>(), false, DateTimeOffset.UtcNow));
    }
}
