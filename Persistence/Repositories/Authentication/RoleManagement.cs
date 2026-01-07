using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Identity;
using Domain.Interfaces.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Persistence.Repositories.Authentication
{
    public class RoleManagement(UserManager<User> userManager) : IRoleManagement
    {
        public async Task<bool> AddUserToRoleAsync(User user, string roleName)
        {
            var result = await userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }

        public async Task<string?> GetUserRoleAsync(string userEmail)
        {
            var user = await userManager.FindByEmailAsync(userEmail);
            return (await userManager.GetRolesAsync(user!)).FirstOrDefault();
        }
    }
}