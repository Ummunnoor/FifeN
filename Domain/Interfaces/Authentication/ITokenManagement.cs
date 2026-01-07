using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Domain.Interfaces.Authentication
{
    public interface ITokenManagement
    {
        string GetRefreshToken();
        List<Claim> GetUserClaimsFromToken(string email);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);
        Task<string> GetUserIdByRefreshTokenAsync(string refreshToken);
        Task<int> AddRefreshTokenAsync( string refreshToken);
        Task<int> UpdateRefreshTokenAsync(string userId, string refreshToken);
        string GenerateToken(List<Claim> claims);


    }
}