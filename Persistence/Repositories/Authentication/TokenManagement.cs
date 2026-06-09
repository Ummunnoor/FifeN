using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain.Entities.Identity;
using Domain.Interfaces.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Persistence.Repositories.Authentication
{
    public class TokenManagement : ITokenManagement
    {
        private readonly FifeNDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public TokenManagement(FifeNDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public string GenerateToken(List<Claim> claims)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "30");

            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT signing key is missing.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwtClaims = new List<Claim>(claims)
            {
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: jwtClaims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public List<Claim> GetUserClaimsFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.ToList();
        }


        public string GetRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        public async Task<int> AddRefreshTokenAsync(string userId, string refreshToken)
        {
            var token = new RefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            _dbContext.RefreshTokens.Add(token);
            return await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            var hash = HashToken(refreshToken);

            return await _dbContext.RefreshTokens.AnyAsync(rt =>
                rt.TokenHash == hash &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<string> GetUserIdByRefreshTokenAsync(string refreshToken)
        {
            var hash = HashToken(refreshToken);

            var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt =>
                rt.TokenHash == hash &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);

            return token?.UserId
                ?? throw new InvalidOperationException("Refresh token not found or invalid.");
        }

        public async Task<int> UpdateRefreshTokenAsync(string userId, string newRefreshToken)
        {
            var token = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt =>
                    rt.UserId == userId &&
                    !rt.IsRevoked);

            if (token == null)
                return -1;

            token.TokenHash = HashToken(newRefreshToken);
            token.ExpiresAt = DateTime.UtcNow.AddDays(7);

            return await _dbContext.SaveChangesAsync();
        }

        public async Task<int> RevokeRefreshTokenAsync(string refreshToken)
        {
            var hash = HashToken(refreshToken);

            var token = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            if (token == null)
                return -1;

            token.IsRevoked = true;
            return await _dbContext.SaveChangesAsync();
        }

       
        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            return Convert.ToBase64String(sha256.ComputeHash(bytes));
        }
    }
}
