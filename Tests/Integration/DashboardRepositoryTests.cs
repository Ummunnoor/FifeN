using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Enums;
using Persistence.Modules.Admin;
using Xunit;

namespace FifeN.Tests.Integration
{
    /// <summary>
    /// Integration tests for <see cref="DashboardRepository"/> against real PostgreSQL. These exercise
    /// the metrics that could not be validated in-memory: <c>GROUP BY</c> over a navigation
    /// (Category.Name) and an owned type (Location.State), and the cross-discovery rate ratio.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    public sealed class DashboardRepositoryTests : IAsyncLifetime
    {
        private readonly PostgresFixture _fixture;

        public DashboardRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

        public Task InitializeAsync() => _fixture.ResetAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task GetDashboardAsync_WithLiveListings_GroupsTopCategoriesAndStates()
        {
            await using (var db = _fixture.CreateContext())
            {
                var user = TestData.VendorUser();
                var vendor = TestData.Vendor(user.Id);
                var fashion = TestData.Category("Fashion", "fashion");
                var food = TestData.Category("Food", "food");
                db.Users.Add(user);
                db.VendorProfiles.Add(vendor);
                db.Categories.AddRange(fashion, food);

                // Fashion x2 in Lagos, Food x1 in Oyo — plus one non-Live that must be ignored.
                db.Products.Add(TestData.Product(vendor.Id, fashion.Id, "Shirt", "x", NigerianState.Lagos, "Ikeja"));
                db.Products.Add(TestData.Product(vendor.Id, fashion.Id, "Jeans", "x", NigerianState.Lagos, "Ikeja"));
                db.Products.Add(TestData.Product(vendor.Id, food.Id, "Rice", "x", NigerianState.Oyo, "Ibadan"));
                db.Products.Add(TestData.Product(vendor.Id, food.Id, "Archived", "x", NigerianState.Oyo, "Ibadan", status: ListingStatus.Archived));
                await db.SaveChangesAsync();
            }

            await using var query = _fixture.CreateContext();
            var result = await new DashboardRepository(query).GetDashboardAsync(CancellationToken.None);

            // Top category by Live-listing count is Fashion (2), then Food (1).
            Assert.Equal("Fashion", result.TopCategories.First().Name);
            Assert.Equal(2, result.TopCategories.First().Count);
            Assert.Contains(result.TopCategories, c => c.Name == "Food" && c.Count == 1);

            // Top location (Location.State, an owned type) is Lagos (2), then Oyo (1).
            Assert.Equal("Lagos", result.TopLocations.First().Name);
            Assert.Equal(2, result.TopLocations.First().Count);
            Assert.Contains(result.TopLocations, l => l.Name == "Oyo" && l.Count == 1);

            Assert.Equal(3, result.ActiveListings); // the Archived listing is excluded
        }

        [Fact]
        public async Task GetDashboardAsync_WithCrossDiscoveryInteractions_ComputesRate()
        {
            await using (var db = _fixture.CreateContext())
            {
                var user = TestData.VendorUser();
                var vendor = TestData.Vendor(user.Id);
                var category = TestData.Category("Fashion", "fashion");
                var buyer = TestData.Buyer();
                var product = TestData.Product(vendor.Id, category.Id, "Shirt", "x", NigerianState.Lagos, "Ikeja");
                db.Users.AddRange(user, buyer);
                db.VendorProfiles.Add(vendor);
                db.Categories.Add(category);
                db.Products.Add(product);

                // 3 of 4 interactions are cross-discovery → rate 0.75.
                db.Interactions.Add(TestData.Interaction(buyer.Id, vendor.Id, product.Id, isCrossDiscovery: true));
                db.Interactions.Add(TestData.Interaction(buyer.Id, vendor.Id, product.Id, isCrossDiscovery: true));
                db.Interactions.Add(TestData.Interaction(buyer.Id, vendor.Id, product.Id, isCrossDiscovery: true));
                db.Interactions.Add(TestData.Interaction(buyer.Id, vendor.Id, product.Id, isCrossDiscovery: false));
                await db.SaveChangesAsync();
            }

            await using var query = _fixture.CreateContext();
            var result = await new DashboardRepository(query).GetDashboardAsync(CancellationToken.None);

            Assert.Equal(4, result.Interactions30d);
            Assert.Equal(0.75d, result.CrossDiscoveryRate);
        }

        [Fact]
        public async Task GetDashboardAsync_WithNoData_ReturnsZeroedMetricsWithoutThrowing()
        {
            await using var query = _fixture.CreateContext();
            var result = await new DashboardRepository(query).GetDashboardAsync(CancellationToken.None);

            Assert.Equal(0, result.ActiveListings);
            Assert.Equal(0d, result.OtpSuccessRate);
            Assert.Equal(0d, result.CrossDiscoveryRate);
            Assert.Empty(result.TopCategories);
            Assert.Empty(result.TopLocations);
        }
    }
}
