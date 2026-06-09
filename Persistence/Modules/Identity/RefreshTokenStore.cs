using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Identity
{
    /// <summary>Persists hashed, rotating refresh tokens. Raw tokens are returned to the caller only once.</summary>
    public sealed class RefreshTokenStore(FifeNDbContext db, ISecureHasher hasher) : IRefreshTokenStore
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

        public async Task<string> IssueAsync(Guid userId, string? ipAddress, CancellationToken ct)
        {
            var raw = GenerateRawToken();
            var now = DateTimeOffset.UtcNow;
            db.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                TokenHash = hasher.Hash(raw),
                ExpiresAtUtc = now + Lifetime,
                CreatedByIp = ipAddress,
                CreatedAtUtc = now
            });
            await db.SaveChangesAsync(ct);
            return raw;
        }

        public async Task<(Guid UserId, string Token)?> RotateAsync(
            string oldRawToken, string? ipAddress, CancellationToken ct)
        {
            var hash = hasher.Hash(oldRawToken);
            var existing = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

            if (existing is null || !existing.IsActive)
                return null;

            existing.RevokedAtUtc = DateTimeOffset.UtcNow;
            var raw = await IssueAsync(existing.UserId, ipAddress, ct); // also saves the revocation
            return (existing.UserId, raw);
        }

        public async Task RevokeAsync(string rawToken, CancellationToken ct)
        {
            var hash = hasher.Hash(rawToken);
            var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
            if (token is { RevokedAtUtc: null })
            {
                token.RevokedAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }

        private static string GenerateRawToken() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
