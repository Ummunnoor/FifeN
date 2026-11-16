using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Modules.Identity;
using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Persistence.Modules.Identity
{
    /// <summary>Issues signed, short-lived JWT access tokens carrying role and capability claims.</summary>
    public sealed class TokenService(IConfiguration configuration) : ITokenService
    {
        public (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(
            User user, IReadOnlyCollection<string> roles, bool mfaSatisfied, DateTimeOffset sessionStartedAtUtc)
        {
            var key = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT signing key (Jwt:Key) is not configured.");
            if (!int.TryParse(configuration["Jwt:ExpiryMinutes"], out var expiryMinutes)
                || expiryMinutes != AuthenticationTokenPolicy.AccessTokenLifetimeMinutes)
                throw new InvalidOperationException(
                    $"Jwt:ExpiryMinutes must be configured as {AuthenticationTokenPolicy.AccessTokenLifetimeMinutes} to match the authentication policy.");

            var now = DateTimeOffset.UtcNow;
            if (!AuthenticationTokenPolicy.IsSessionWithinAbsoluteLifetime(sessionStartedAtUtc, now))
                throw new InvalidOperationException("Cannot issue an access token outside the absolute session lifetime.");
            var absoluteExpiry = sessionStartedAtUtc + AuthenticationTokenPolicy.AbsoluteSessionLifetime;
            var expiresAtUtc = Min(
                now.AddMinutes(AuthenticationTokenPolicy.AccessTokenLifetimeMinutes), absoluteExpiry);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("name", user.DisplayName),
                new("is_owner", user.IsOwner ? "true" : "false"),
                new("is_vendor", user.IsVendor ? "true" : "false"),
                new("is_admin", user.IsAdmin ? "true" : "false"),
                new("session_started_at", sessionStartedAtUtc.ToString("O", CultureInfo.InvariantCulture))
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            if (mfaSatisfied)
                claims.Add(new Claim("amr", "mfa"));

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAtUtc.UtcDateTime,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
        }

        public string CreateMfaSetupToken(User user, DateTimeOffset sessionStartedAtUtc)
            => CreateRestrictedMfaToken(user, sessionStartedAtUtc, "mfa_setup", 10);

        public string CreateMfaChallengeToken(User user, DateTimeOffset sessionStartedAtUtc)
            => CreateRestrictedMfaToken(user, sessionStartedAtUtc, "mfa_login", 5);

        private string CreateRestrictedMfaToken(
            User user, DateTimeOffset sessionStartedAtUtc, string tokenUse, int lifetimeMinutes)
        {
            var key = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT signing key (Jwt:Key) is not configured.");
            var now = DateTimeOffset.UtcNow;
            if (!AuthenticationTokenPolicy.IsSessionWithinAbsoluteLifetime(sessionStartedAtUtc, now))
                throw new InvalidOperationException("Cannot issue a setup token outside the absolute session lifetime.");

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("token_use", tokenUse),
                new Claim("session_started_at", sessionStartedAtUtc.ToString("O", CultureInfo.InvariantCulture))
            };
            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: now.AddMinutes(lifetimeMinutes).UtcDateTime,
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static DateTimeOffset Min(DateTimeOffset first, DateTimeOffset second) =>
            first < second ? first : second;
    }
}
