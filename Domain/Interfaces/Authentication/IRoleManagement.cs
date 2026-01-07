using Domain.Entities.Identity;

namespace Domain.Interfaces.Authentication
{
    public interface IRoleManagement
    {
        Task<string?> GetUserRoleAsync(string userEmail);
        Task<bool> AddUserToRoleAsync(User user, string roleName);
    }

    
}