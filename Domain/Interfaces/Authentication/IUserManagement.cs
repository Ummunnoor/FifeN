using System.Security.Claims;
using Domain.Entities.Identity;

namespace Domain.Interfaces.Authentication
{
    public interface IUserManagement
    {
        Task<bool> CreateUserAsync(User user);
        Task<bool> LoginUserAsync(User user);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> GetUserByIdAsync(string id);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<int> RemoveUserByEmailAsync(string email);
        Task<List<Claim>> GetUserClaimsAsync(string userEmail);
    }
}
