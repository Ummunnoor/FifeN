using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Seeding;
using Xunit;

namespace FifeN.Tests.Integration
{
    /// <summary>
    /// End-to-end test of <see cref="DbSeeder"/> against real PostgreSQL — the most complex write path
    /// in the system (roles, admins, categories, vendors, listings with jsonb attributes, interactions,
    /// reviews). Also pins the idempotency guarantee: running it twice must not duplicate rows.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    public sealed class DbSeederTests : IAsyncLifetime
    {
        private readonly PostgresFixture _fixture;

        public DbSeederTests(PostgresFixture fixture) => _fixture = fixture;

        public Task InitializeAsync() => _fixture.ResetAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        private async Task SeedAsync()
        {
            await using var provider = _fixture.BuildServiceProvider();
            using var scope = provider.CreateScope();
            await DbSeeder.SeedAsync(scope.ServiceProvider, CancellationToken.None);
        }

        [Fact]
        public async Task SeedAsync_OnEmptyDatabase_PopulatesTheDemoDataset()
        {
            await SeedAsync();

            await using var db = _fixture.CreateContext();

            // Roles: every AppRole value is seeded.
            Assert.Equal(Enum.GetNames<AppRole>().Length, await db.Roles.CountAsync());

            // Admins (3) + vendors (5) + buyers (2) = 10 users.
            Assert.Equal(3, await db.Users.CountAsync(u => u.IsAdmin));
            Assert.Equal(5, await db.VendorProfiles.CountAsync());
            Assert.Equal(10, await db.Users.CountAsync());

            Assert.Equal(5, await db.Categories.CountAsync());
            Assert.Equal(10, await db.Products.CountAsync());
            Assert.Equal(6, await db.Interactions.CountAsync());
            Assert.Equal(4, await db.Reviews.IgnoreQueryFilters().CountAsync());

            // The founder is flagged as owner + admin with MFA enabled (admin-surface bootstrap).
            var owner = await db.Users.SingleAsync(u => u.IsOwner);
            Assert.True(owner.IsAdmin);
            Assert.True(owner.TwoFactorEnabled);

            // Cross-discovery interactions exist so the dashboard headline is non-zero.
            Assert.True(await db.Interactions.AnyAsync(i => i.IsCrossDiscovery));
        }

        [Fact]
        public async Task SeedAsync_RunTwice_IsIdempotent()
        {
            await SeedAsync();
            await SeedAsync();

            await using var db = _fixture.CreateContext();

            Assert.Equal(10, await db.Users.CountAsync());
            Assert.Equal(5, await db.VendorProfiles.CountAsync());
            Assert.Equal(5, await db.Categories.CountAsync());
            Assert.Equal(10, await db.Products.CountAsync());
            Assert.Equal(6, await db.Interactions.CountAsync());
            Assert.Equal(4, await db.Reviews.IgnoreQueryFilters().CountAsync());
        }
    }
}
