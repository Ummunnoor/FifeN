using System.Security.Claims;
using Domain.Entities.Identity;

namespace Domain.Interfaces.Authentication
{
    public interface IUserManagement
    {
        Task<bool> CreateUserAsync(User user, string password);
        Task<User?> LoginUserAsync(string email, string password);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> GetUserByIdAsync(string id);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<bool> RemoveUserByEmailAsync(string email);
        Task<List<Claim>> GetUserClaimsAsync(User user);
    }
}
