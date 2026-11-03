using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Modules.Identity;
using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Identity
{
    /// <summary>Persists hashed, rotating refresh tokens. Raw tokens are returned to the caller only once.</summary>
    public sealed class RefreshTokenStore(FifeNDbContext db, ISecureHasher hasher) : IRefreshTokenStore
    {
        public async Task<(string Token, DateTimeOffset SessionStartedAtUtc)> IssueAsync(
            Guid userId, string? ipAddress, bool rememberDevice, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            var raw = GenerateRawToken();
            db.RefreshTokens.Add(CreateRefreshToken(
                userId, raw, ipAddress, rememberDevice, now, now));
            await db.SaveChangesAsync(ct);
            return (raw, now);
        }

        public async Task<(Guid UserId, string Token, DateTimeOffset SessionStartedAtUtc)?> RotateAsync(
            string oldRawToken, string? ipAddress, CancellationToken ct)
        {
            var hash = hasher.Hash(oldRawToken);
            var existing = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

            if (existing is null)
                return null;

            var now = DateTimeOffset.UtcNow;
            if (!AuthenticationTokenPolicy.IsSessionWithinAbsoluteLifetime(existing.SessionStartedAtUtc, now))
            {
                if (existing.RevokedAtUtc is null)
                {
                    existing.RevokedAtUtc = now;
                    try
                    {
                        await db.SaveChangesAsync(ct);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        db.Entry(existing).State = EntityState.Detached;
                    }
                }

                return null;
            }

            if (!existing.IsActive)
                return null;

            var raw = GenerateRawToken();
            var replacement = CreateRefreshToken(
                existing.UserId,
                raw,
                ipAddress,
                existing.RememberDevice,
                existing.SessionStartedAtUtc,
                now);
            existing.RevokedAtUtc = now;
            db.RefreshTokens.Add(replacement);

            try
            {
                await db.SaveChangesAsync(ct);
                return (existing.UserId, raw, existing.SessionStartedAtUtc);
            }
            catch (DbUpdateConcurrencyException)
            {
                db.Entry(existing).State = EntityState.Detached;
                db.Entry(replacement).State = EntityState.Detached;
                return null;
            }
        }

        public async Task RevokeAsync(string rawToken, CancellationToken ct)
        {
            var hash = hasher.Hash(rawToken);
            var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
            if (token is { RevokedAtUtc: null })
            {
                token.RevokedAtUtc = DateTimeOffset.UtcNow;
                try
                {
                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateConcurrencyException)
                {
                    db.Entry(token).State = EntityState.Detached;
                }
            }
        }

        private static string GenerateRawToken() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        private RefreshToken CreateRefreshToken(
            Guid userId,
            string rawToken,
            string? ipAddress,
            bool rememberDevice,
            DateTimeOffset sessionStartedAtUtc,
            DateTimeOffset now)
        {
            var lifetime = rememberDevice
                ? AuthenticationTokenPolicy.RememberedRefreshTokenLifetime
                : AuthenticationTokenPolicy.StandardRefreshTokenLifetime;
            var absoluteExpiry = sessionStartedAtUtc + AuthenticationTokenPolicy.AbsoluteSessionLifetime;

            return new RefreshToken
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                TokenHash = hasher.Hash(rawToken),
                ExpiresAtUtc = Min(now + lifetime, absoluteExpiry),
                SessionStartedAtUtc = sessionStartedAtUtc,
                RememberDevice = rememberDevice,
                CreatedByIp = ipAddress,
                CreatedAtUtc = now
            };
        }

        private static DateTimeOffset Min(DateTimeOffset first, DateTimeOffset second) =>
            first < second ? first : second;
    }
}
