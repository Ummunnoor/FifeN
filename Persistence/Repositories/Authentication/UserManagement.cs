using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Services.Interfaces.Logging;
using Domain.Entities.Identity;
using Domain.Interfaces.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories.Authentication
{
    public class UserManagement(IRoleManagement roleManagement, UserManager<User> userManager, SignInManager<User> signInManager, IAppLogger<UserManagement> logger, FifeNDbContext dbContext) : IUserManagement
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

        public async Task<List<Claim>> GetUserClaimsAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("FullName", user.UserName ?? string.Empty)
            };
            var role = await userManager.GetRolesAsync(user);
            if (role.Count != 1)
            {
                 logger.LogWarning(
                    $"User {user.Id} has invalid role count: {role.Count}"
                );
                return new List<Claim>();
            }
            claims.Add(new Claim(ClaimTypes.Role, role.First()));
            return claims;
        }

        public async Task<User?> LoginUserAsync(string email, string password)
        {
            var _user = await GetUserByEmailAsync(email);
            if (_user is null) return null;
            var roleName = await roleManagement.GetUserRoleAsync(_user.Email!);
            if (roleName == null) return null;
            var result = await signInManager.CheckPasswordSignInAsync(_user, password, lockoutOnFailure: false);
            return result.Succeeded ? _user : null;
        }

        public async Task<bool> RemoveUserByEmailAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return false;
            var result = await userManager.DeleteAsync(user);
            return result.Succeeded;
        }
    }
}