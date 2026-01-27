using Domain.Entities.Enums;
using Domain.Entities.Identity;

namespace Domain.Interfaces.Authentication
{
    public interface IRoleManagement
    {
        Task<AppRole?> GetUserRoleAsync(string userEmail);
        Task AssignRoleAsync(User user, AppRole role);
        Task ReplaceUserRoleAsync(User user, AppRole newRole);
    }
}