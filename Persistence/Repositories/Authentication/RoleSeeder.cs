using System;
using System.Threading.Tasks;
using Domain.Entities.Enums;
using Microsoft.AspNetCore.Identity;

namespace Persistence.Repositories.Authentication
{
    /// <summary>Idempotently seeds the application's RBAC roles (Guid-keyed).</summary>
    public static class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            foreach (var role in Enum.GetNames<AppRole>())
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }
}
