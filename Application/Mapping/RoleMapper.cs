using Domain.Entities.Enums;

namespace Application.Mapping
{
    public static class RoleMapper
    {
        public static AppRole FromIdentityRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Role name cannot be null or empty.", nameof(roleName));

            return roleName switch
            {
                nameof(AppRole.User) => AppRole.User,
                nameof(AppRole.Admin) => AppRole.Admin,
                nameof(AppRole.Vendor) => AppRole.Vendor,
                nameof(AppRole.Support) => AppRole.Support,
                _ => throw new InvalidOperationException(
                    $"Unknown role name '{roleName}'. Ensure roles are correctly seeded.")
            };
        }
    }
}
