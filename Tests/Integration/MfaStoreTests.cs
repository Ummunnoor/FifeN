using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Persistence.Modules.Identity;
using Xunit;

namespace FifeN.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class MfaStoreTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public MfaStoreTests(PostgresFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ConcurrentEnrollmentRequests_ReturnSameUriAndPersistOneAuthenticatorKey()
    {
        var email = "mfa-admin@example.com";
        var userId = Guid.CreateVersion7();
        await using (var provider = BuildRetryingIdentityProvider())
        await using (var scope = provider.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var create = await users.CreateAsync(new User
            {
                Id = userId,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "MFA",
                LastName = "Admin"
            });
            Assert.True(create.Succeeded, string.Join("; ", create.Errors.Select(e => e.Description)));
        }

        var uris = await Task.WhenAll(Enumerable.Range(0, 6).Select(async _ =>
        {
            await using var provider = BuildRetryingIdentityProvider();
            await using var scope = provider.CreateAsyncScope();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var db = scope.ServiceProvider.GetRequiredService<FifeNDbContext>();
            return await new MfaStore(manager, db)
                .ResetAndBuildAuthenticatorUriAsync(userId, CancellationToken.None);
        }));

        Assert.All(uris, uri => Assert.Equal(uris[0], uri));
        await using var verifyDb = _fixture.CreateContext();
        var storedKeyCount = await verifyDb.UserTokens.CountAsync(t =>
            t.UserId == userId && t.LoginProvider == "[AspNetUserStore]" && t.Name == "AuthenticatorKey");
        Assert.Equal(1, storedKeyCount);
    }

    private ServiceProvider BuildRetryingIdentityProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<FifeNDbContext>(options => options.UseNpgsql(
            _fixture.ConnectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure();
                npgsql.ConfigureDataSource(dataSource => dataSource.EnableDynamicJson());
            }));
        services.AddIdentityCore<User>(options => options.User.RequireUniqueEmail = false)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<FifeNDbContext>()
            .AddDefaultTokenProviders();
        return services.BuildServiceProvider();
    }
}
