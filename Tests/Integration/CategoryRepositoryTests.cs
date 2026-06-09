using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Catalog;
using Persistence.Modules.Catalog;
using Xunit;

namespace FifeN.Tests.Integration
{
    /// <summary>
    /// Integration tests for <see cref="CategoryRepository"/> against a real PostgreSQL database, so the
    /// active-only filter and ordering are exercised through actual SQL rather than an in-memory shim.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    public sealed class CategoryRepositoryTests : IAsyncLifetime
    {
        private readonly PostgresFixture _fixture;

        public CategoryRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

        public Task InitializeAsync() => _fixture.ResetAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        private static Category Category(string name, string slug, bool isActive, int sortOrder) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            IsActive = isActive,
            SortOrder = sortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        [Fact]
        public async Task GetActiveAsync_WithMixedCategories_ReturnsOnlyActiveOrderedBySortOrder()
        {
            await using (var db = _fixture.CreateContext())
            {
                db.Categories.AddRange(
                    Category("Beauty", "beauty", isActive: true, sortOrder: 2),
                    Category("Fashion", "fashion", isActive: true, sortOrder: 1),
                    Category("Archived", "archived", isActive: false, sortOrder: 0));
                await db.SaveChangesAsync();
            }

            await using var query = _fixture.CreateContext();
            var repo = new CategoryRepository(query);

            var result = await repo.GetActiveAsync(CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Collection(result,
                c => Assert.Equal("fashion", c.Slug),   // sortOrder 1 first
                c => Assert.Equal("beauty", c.Slug));    // sortOrder 2 second
            Assert.DoesNotContain(result, c => c.Slug == "archived");
        }

        [Fact]
        public async Task GetAsync_WhenCategoryIsInactive_ReturnsNull()
        {
            var inactive = Category("Hidden", "hidden", isActive: false, sortOrder: 0);
            await using (var db = _fixture.CreateContext())
            {
                db.Categories.Add(inactive);
                await db.SaveChangesAsync();
            }

            await using var query = _fixture.CreateContext();
            var repo = new CategoryRepository(query);

            var result = await repo.GetAsync(inactive.Id, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_WhenCategoryIsActive_ReturnsCategory()
        {
            var active = Category("Home", "home", isActive: true, sortOrder: 0);
            await using (var db = _fixture.CreateContext())
            {
                db.Categories.Add(active);
                await db.SaveChangesAsync();
            }

            await using var query = _fixture.CreateContext();
            var repo = new CategoryRepository(query);

            var result = await repo.GetAsync(active.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("home", result!.Slug);
        }
    }
}
