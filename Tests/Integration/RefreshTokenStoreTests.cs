using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence;
using Persistence.Modules.Identity;
using Xunit;

namespace FifeN.Tests.Integration
{
    [Collection(PostgresCollection.Name)]
    public sealed class RefreshTokenStoreTests : IAsyncLifetime
    {
        private readonly PostgresFixture _fixture;
        private readonly ISecureHasher _hasher;

        public RefreshTokenStoreTests(PostgresFixture fixture)
        {
            _fixture = fixture;
            _hasher = new Sha256SecureHasher(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:HashKey"] = "refresh-token-test-hash-key"
                })
                .Build());
        }

        public Task InitializeAsync() => _fixture.ResetAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task IssueAsync_NormalDevice_ExpiresAfterSevenDays()
        {
            await using var db = _fixture.CreateContext();
            var userId = await CreateUserAsync(db);
            var store = new RefreshTokenStore(db, _hasher);

            var (token, _) = await store.IssueAsync(userId, null, false, CancellationToken.None);
            var stored = await db.RefreshTokens.SingleAsync(t => t.TokenHash == _hasher.Hash(token));

            Assert.False(stored.RememberDevice);
            Assert.Equal(stored.SessionStartedAtUtc, stored.CreatedAtUtc);
            Assert.Equal(TimeSpan.FromDays(7), stored.ExpiresAtUtc - stored.CreatedAtUtc);
        }

        [Fact]
        public async Task IssueAsync_RememberedDevice_ExpiresAfterThirtyDays()
        {
            await using var db = _fixture.CreateContext();
            var userId = await CreateUserAsync(db);
            var store = new RefreshTokenStore(db, _hasher);

            var (token, _) = await store.IssueAsync(userId, null, true, CancellationToken.None);
            var stored = await db.RefreshTokens.SingleAsync(t => t.TokenHash == _hasher.Hash(token));

            Assert.True(stored.RememberDevice);
            Assert.Equal(TimeSpan.FromDays(30), stored.ExpiresAtUtc - stored.CreatedAtUtc);
        }

        [Fact]
        public async Task RotateAsync_RememberedDevice_RevokesOldTokenAndPreservesSessionLimit()
        {
            await using var db = _fixture.CreateContext();
            var userId = await CreateUserAsync(db);
            var store = new RefreshTokenStore(db, _hasher);
            var (originalToken, _) = await store.IssueAsync(userId, null, true, CancellationToken.None);
            var original = await db.RefreshTokens.SingleAsync(t => t.TokenHash == _hasher.Hash(originalToken));

            var rotated = await store.RotateAsync(originalToken, null, CancellationToken.None);

            Assert.NotNull(rotated);
            Assert.Equal(userId, rotated.Value.UserId);
            Assert.NotEqual(originalToken, rotated.Value.Token);

            var oldRecord = await db.RefreshTokens.SingleAsync(t => t.Id == original.Id);
            var replacement = await db.RefreshTokens.SingleAsync(t => t.TokenHash == _hasher.Hash(rotated.Value.Token));
            Assert.NotNull(oldRecord.RevokedAtUtc);
            Assert.True(replacement.IsActive);
            Assert.True(replacement.RememberDevice);
            Assert.Equal(original.SessionStartedAtUtc, replacement.SessionStartedAtUtc);
            Assert.Equal(original.SessionStartedAtUtc.AddDays(30), replacement.ExpiresAtUtc);
        }

        [Fact]
        public async Task RotateAsync_AbsoluteSessionLifetimeExceeded_RevokesAndRejectsToken()
        {
            await using var db = _fixture.CreateContext();
            var userId = await CreateUserAsync(db);
            var store = new RefreshTokenStore(db, _hasher);
            var (token, _) = await store.IssueAsync(userId, null, true, CancellationToken.None);
            var stored = await db.RefreshTokens.SingleAsync(t => t.TokenHash == _hasher.Hash(token));
            stored.SessionStartedAtUtc = DateTimeOffset.UtcNow.AddDays(-31);
            stored.ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1);
            await db.SaveChangesAsync();

            var rotated = await store.RotateAsync(token, null, CancellationToken.None);

            Assert.Null(rotated);
            Assert.NotNull(stored.RevokedAtUtc);
            Assert.Single(await db.RefreshTokens.ToListAsync());
        }

        [Fact]
        public async Task RotateAsync_ConcurrentRefreshes_AllowsOnlyOneRotation()
        {
            await using var setupDb = _fixture.CreateContext();
            var userId = await CreateUserAsync(setupDb);
            var tokenStore = new RefreshTokenStore(setupDb, _hasher);
            var (token, _) = await tokenStore.IssueAsync(userId, null, false, CancellationToken.None);

            await using var firstDb = _fixture.CreateContext();
            await using var secondDb = _fixture.CreateContext();
            var firstStore = new RefreshTokenStore(firstDb, _hasher);
            var secondStore = new RefreshTokenStore(secondDb, _hasher);

            var results = await Task.WhenAll(
                firstStore.RotateAsync(token, null, CancellationToken.None),
                secondStore.RotateAsync(token, null, CancellationToken.None));

            Assert.Single(results, result => result is not null);
            await using var verifyDb = _fixture.CreateContext();
            Assert.Equal(2, await verifyDb.RefreshTokens.CountAsync());
            Assert.Equal(1, await verifyDb.RefreshTokens.CountAsync(t => t.RevokedAtUtc == null));
        }

        private static async Task<Guid> CreateUserAsync(FifeNDbContext db)
        {
            var now = DateTimeOffset.UtcNow;
            var email = $"refresh-token-{Guid.NewGuid():N}@example.com";
            var user = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                FirstName = "Refresh",
                LastName = "Token",
                Status = UserStatus.Active,
                CreatedAtUtc = now
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        }
    }
}
