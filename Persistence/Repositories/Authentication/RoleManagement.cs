using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Domain.Interfaces.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Persistence.Repositories.Authentication
{
    public class RoleManagement(UserManager<User> userManager, RoleManager<IdentityRole> roleManager) : IRoleManagement
    {
        public async Task AssignRoleAsync(User user, AppRole role)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var roleName = role.ToString();

            if (!await roleManager.RoleExistsAsync(roleName))
                throw new InvalidOperationException($"Role '{roleName}' does not exist.");

            var existingRoles = await userManager.GetRolesAsync(user);
            if (existingRoles.Any())
                throw new InvalidOperationException("User already has a role assigned.");

            var result = await userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );
        }


        public async Task<AppRole?> GetUserRoleAsync(string userEmail)
        {
            var user = await userManager.FindByEmailAsync(userEmail);
            if (user == null) return null;
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Any()) return null;
            if (roles.Count > 1)
            {
                throw new InvalidOperationException(
                    $"User '{userEmail}' has multiple roles. Expected exactly one.");
            }

            return Enum.Parse<AppRole>(roles.Single(), ignoreCase: false);
        }

        public async Task ReplaceUserRoleAsync(User user, AppRole newRole)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var roleName = newRole.ToString();

            if (!await roleManager.RoleExistsAsync(roleName))
                throw new InvalidOperationException($"Role '{roleName}' does not exist.");

            var existingRoles = await userManager.GetRolesAsync(user);
            if (!existingRoles.Any())
                throw new InvalidOperationException("User has no role assigned.");

            var result = await userManager.RemoveFromRolesAsync(user, existingRoles);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );

            result = await userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );
        }
    }

}