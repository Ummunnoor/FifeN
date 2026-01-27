

using Domain.Interfaces.Authentication;

namespace Persistence.Repositories.Authentication
{
    public class Sha256TokenHasher : ITokenHasher
    {
        public string Hash(string token)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}