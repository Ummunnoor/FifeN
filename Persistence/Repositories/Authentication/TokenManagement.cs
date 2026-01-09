using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Domain.Entities.Identity;
using Domain.Interfaces.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Persistence.Repositories.Authentication
{
    public class TokenManagement(FifeNDbContext dbContext, IConfiguration configuration) : ITokenManagement
    {
        public async Task<int> AddRefreshTokenAsync(string userId, string refreshToken)
        {
            dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                Token = refreshToken
            });
            return await dbContext.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public string GenerateToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddMinutes(30);
            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: cred
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GetRefreshToken()
        {
            const int byteSize = 64;
            byte[] randomBytes = new byte[byteSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        public List<Claim> GetUserClaimsFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            if (jwtToken != null)
            return jwtToken.Claims.ToList();
            else return [];
        }

        public async Task<string> GetUserIdByRefreshTokenAsync(string refreshToken)
        {
            var token = await dbContext.RefreshTokens
         .FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            return token?.UserId!;
        }

        public async Task<int> UpdateRefreshTokenAsync(string userId, string refreshToken)
        {
            var user = dbContext.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);
            if (user == null) return -1;
            user.Token = refreshToken;
            return await dbContext.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            var user = await dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            return user != null;
        }
    }
}