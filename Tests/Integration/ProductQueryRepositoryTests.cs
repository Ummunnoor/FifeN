using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Discovery.DTOs;
using Domain.Entities.Enums;
using Persistence.Modules.Discovery;
using Xunit;

namespace FifeN.Tests.Integration
{
    /// <summary>
    /// Integration tests for <see cref="ProductQueryRepository"/> against real PostgreSQL — the only
    /// way to exercise the generated <c>tsvector</c> full-text search, the <c>ILIKE</c> fallback, and
    /// the visibility join (Live listing + active vendor) through actual SQL.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    public sealed class ProductQueryRepositoryTests : IAsyncLifetime
    {
        private readonly PostgresFixture _fixture;

        public ProductQueryRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

        public Task InitializeAsync() => _fixture.ResetAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>Seeds one verified, active vendor with the given listings and returns nothing.</summary>
        private async Task SeedVendorWithProductsAsync(params (string Title, string Desc, NigerianState State, string City, ListingStatus Status)[] products)
        {
            await using var db = _fixture.CreateContext();
            var user = TestData.VendorUser();
            var vendor = TestData.Vendor(user.Id);
            var category = TestData.Category("Fashion", "fashion");
            db.Users.Add(user);
            db.VendorProfiles.Add(vendor);
            db.Categories.Add(category);
            foreach (var p in products)
                db.Products.Add(TestData.Product(vendor.Id, category.Id, p.Title, p.Desc, p.State, p.City, status: p.Status));
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task SearchAsync_WithFullTextTerm_MatchesViaTsVector()
        {
            await SeedVendorWithProductsAsync(
                ("Red Nike Running Shoes", "Comfortable trainers for the road", NigerianState.Lagos, "Ikeja", ListingStatus.Live),
                ("Blue Ceramic Vase", "Handmade pottery for the home", NigerianState.Lagos, "Ikeja", ListingStatus.Live));

            await using var db = _fixture.CreateContext();
            var repo = new ProductQueryRepository(db);

            var result = await repo.SearchAsync(new ProductQuery(Q: "running shoes"), CancellationToken.None);

            Assert.Equal(1, result.Total);
            Assert.Equal("Red Nike Running Shoes", result.Items.Single().Title);
        }

        [Fact]
        public async Task SearchAsync_WithPartialTitleFragment_MatchesViaIlikeFallback()
        {
            // "Nik" does not stem to a tsquery lexeme, so a hit here proves the ILIKE fallback works.
            await SeedVendorWithProductsAsync(
                ("Red Nike Running Shoes", "Comfortable trainers", NigerianState.Lagos, "Ikeja", ListingStatus.Live),
                ("Blue Ceramic Vase", "Handmade pottery", NigerianState.Lagos, "Ikeja", ListingStatus.Live));

            await using var db = _fixture.CreateContext();
            var repo = new ProductQueryRepository(db);

            var result = await repo.SearchAsync(new ProductQuery(Q: "Nik"), CancellationToken.None);

            Assert.Equal(1, result.Total);
            Assert.Equal("Red Nike Running Shoes", result.Items.Single().Title);
        }

        [Fact]
        public async Task SearchAsync_WithStateFilter_ReturnsOnlyMatchingState()
        {
            await SeedVendorWithProductsAsync(
                ("Lagos Listing", "x", NigerianState.Lagos, "Ikeja", ListingStatus.Live),
                ("Oyo Listing", "x", NigerianState.Oyo, "Ibadan", ListingStatus.Live));

            await using var db = _fixture.CreateContext();
            var repo = new ProductQueryRepository(db);

            var result = await repo.SearchAsync(new ProductQuery(State: NigerianState.Oyo), CancellationToken.None);

            Assert.Equal(1, result.Total);
            Assert.Equal("Oyo Listing", result.Items.Single().Title);
        }

        [Fact]
        public async Task SearchAsync_WithNonLiveListing_ExcludesIt()
        {
            await SeedVendorWithProductsAsync(
                ("Live Listing", "x", NigerianState.Lagos, "Ikeja", ListingStatus.Live),
                ("Archived Listing", "x", NigerianState.Lagos, "Ikeja", ListingStatus.Archived));

            await using var db = _fixture.CreateContext();
            var repo = new ProductQueryRepository(db);

            var result = await repo.SearchAsync(new ProductQuery(), CancellationToken.None);

            Assert.Equal(1, result.Total);
            Assert.Equal("Live Listing", result.Items.Single().Title);
        }

        [Fact]
        public async Task SearchAsync_WhenVendorIsInactive_ExcludesListing()
        {
            await using (var db = _fixture.CreateContext())
            {
                var user = TestData.VendorUser();
                user.Status = UserStatus.Suspended; // vendor no longer active
                var vendor = TestData.Vendor(user.Id);
                var category = TestData.Category("Fashion", "fashion");
                db.Users.Add(user);
                db.VendorProfiles.Add(vendor);
                db.Categories.Add(category);
                db.Products.Add(TestData.Product(vendor.Id, category.Id, "Hidden", "x", NigerianState.Lagos, "Ikeja"));
                await db.SaveChangesAsync();
            }

            await using var query = _fixture.CreateContext();
            var repo = new ProductQueryRepository(query);

            var result = await repo.SearchAsync(new ProductQuery(), CancellationToken.None);

            Assert.Equal(0, result.Total);
            Assert.Empty(result.Items);
        }
    }
}
