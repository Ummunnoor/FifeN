using System;
using System.Security.Cryptography;
using System.Text;
using Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Persistence.Modules.Identity
{
    /// <summary>
    /// Keyed (HMAC-SHA256) implementation of <see cref="ISecureHasher"/> for OTP codes and refresh
    /// tokens. The server-side key acts as a pepper: it means a stolen database alone cannot brute-force
    /// the small 6-digit OTP space offline. The key is read from <c>Security:HashKey</c>, falling back to
    /// the JWT signing key so existing deployments work without new configuration.
    /// </summary>
    public sealed class Sha256SecureHasher : ISecureHasher
    {
        private readonly byte[] _key;

        public Sha256SecureHasher(IConfiguration configuration)
        {
            var key = configuration["Security:HashKey"]
                ?? configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "No hashing key configured (set Security:HashKey or Jwt:Key).");
            _key = Encoding.UTF8.GetBytes(key);
        }

        public string Hash(string value)
        {
            var bytes = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value));
            return Convert.ToBase64String(bytes);
        }
    }
}
