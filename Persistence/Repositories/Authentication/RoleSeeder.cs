using Domain.Entities.Enums;
using Microsoft.AspNetCore.Identity;

namespace Persistence.Repositories.Authentication
{
    public class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in Enum.GetNames<AppRole>())
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

    }
}