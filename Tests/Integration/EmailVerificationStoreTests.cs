using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Modules.Identity;
using Xunit;

namespace FifeN.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class EmailVerificationStoreTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public EmailVerificationStoreTests(PostgresFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TryAddWithinLimitAsync_WithRetryingStrategy_SerializesConcurrentRequests()
    {
        var since = DateTimeOffset.UtcNow.AddHours(-1);
        const string email = "otp-limit@example.com";

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = CreateRetryingContext();
            var store = new EmailVerificationStore(db);
            return await store.TryAddWithinLimitAsync(new EmailVerification
            {
                Id = Guid.CreateVersion7(),
                Email = email,
                CodeHash = "hashed-test-code",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                CreatedAtUtc = DateTimeOffset.UtcNow
            }, since, limit: 1, CancellationToken.None);
        }));

        Assert.Single(results, accepted => accepted);
        await using var verifyDb = CreateRetryingContext();
        Assert.Equal(1, await verifyDb.EmailVerifications.CountAsync(v => v.Email == email));
    }

    private FifeNDbContext CreateRetryingContext() => new(
        new DbContextOptionsBuilder<FifeNDbContext>()
            .UseNpgsql(_fixture.ConnectionString, options =>
            {
                options.EnableRetryOnFailure();
                options.ConfigureDataSource(dataSource => dataSource.EnableDynamicJson());
            })
            .Options);
}
