using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
            User user, IReadOnlyCollection<string> roles, bool mfaSatisfied)
        {
            var key = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT signing key (Jwt:Key) is not configured.");
            var expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 15;
            var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("name", user.DisplayName),
                new("is_owner", user.IsOwner ? "true" : "false"),
                new("is_vendor", user.IsVendor ? "true" : "false"),
                new("is_admin", user.IsAdmin ? "true" : "false")
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
    }
}
