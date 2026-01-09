using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Domain.Entities.Identity;
using Domain.Interfaces.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories.Authentication
{
    public class UserManagement(IRoleManagement roleManagement, UserManager<User> userManager, FifeNDbContext dbContext) : IUserManagement
    {
        public async Task<bool> CreateUserAsync(User user, string password)
        {
            // Check if user exists
            var existingUser = await GetUserByEmailAsync(user.Email!);
            if (existingUser != null)
                return false;

            // Let Identity handle hashing internally
            var result = await userManager.CreateAsync(user, password);

            return result.Succeeded;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await dbContext.Users.ToListAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await userManager.FindByEmailAsync(email);
        }

        public async Task<User> GetUserByIdAsync(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            return user!;
        }

        public async Task<List<Claim>> GetUserClaimsAsync(string userEmail)
        {
            var _user = await GetUserByEmailAsync(userEmail);
            string? roleName = await roleManagement.GetUserRoleAsync(_user!.Email!);
            List<Claim> claims = [
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("FullName", _user.UserName!),
                new Claim(ClaimTypes.NameIdentifier, _user.Id),
                new Claim(ClaimTypes.Email, _user.Email!),
                new Claim(ClaimTypes.Role, roleName!)
            ];
            return claims;
        }

        public async Task<bool> LoginUserAsync(User user)
        {
            var _user = await GetUserByEmailAsync(user.Email!);
            if (_user is null) return false;
            string? roleName = await roleManagement.GetUserRoleAsync(user.Email!);
            if (string.IsNullOrEmpty(roleName)) return false;
            return await userManager.CheckPasswordAsync(_user, user.PasswordHash!);

        }

        public async Task<int> RemoveUserByEmailAsync(string email)
        {
            var _user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            dbContext.Users.Remove(_user!);
            return await dbContext.SaveChangesAsync() > 0 ? 1 : 0;
        }
    }
}