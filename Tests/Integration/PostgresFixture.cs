using System;
using System.Threading.Tasks;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Persistence;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace FifeN.Tests.Integration
{
    /// <summary>
    /// Spins up a throwaway PostgreSQL instance once per test collection via Testcontainers, applies
    /// the EF Core migrations against it, and exposes a Respawn-based <see cref="ResetAsync"/> so each
    /// test starts from a clean schema without paying to recreate the container.
    /// </summary>
    public sealed class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        private Respawner _respawner = null!;
        private NpgsqlConnection _connection = null!;

        public string ConnectionString { get; private set; } = string.Empty;

        public FifeNDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<FifeNDbContext>()
                .UseNpgsql(ConnectionString, o => o.ConfigureDataSource(ds => ds.EnableDynamicJson()))
                .Options);

        /// <summary>
        /// Builds a minimal DI container (DbContext + ASP.NET Identity) wired to the container database,
        /// matching the production identity setup, so service-provider-driven code such as
        /// <c>DbSeeder.SeedAsync</c> can run end to end.
        /// </summary>
        public ServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection(); // required by the default token providers UserManager pulls in
            services.AddDbContext<FifeNDbContext>(o =>
                o.UseNpgsql(ConnectionString, n => n.ConfigureDataSource(ds => ds.EnableDynamicJson())));
            services.AddIdentityCore<User>(o =>
                {
                    o.SignIn.RequireConfirmedEmail = false;
                    o.SignIn.RequireConfirmedPhoneNumber = false;
                    o.User.RequireUniqueEmail = false;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<FifeNDbContext>()
                .AddDefaultTokenProviders();
            return services.BuildServiceProvider();
        }

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();

            await using (var db = CreateContext())
                await db.Database.MigrateAsync();

            _connection = new NpgsqlConnection(ConnectionString);
            await _connection.OpenAsync();
            _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                // Never wipe the migrations ledger; the schema is created once.
                TablesToIgnore = [new Respawn.Graph.Table("__EFMigrationsHistory")],
                SchemasToInclude = ["public"]
            });
        }

        /// <summary>Deletes all data (respecting FK order) so the next test sees an empty database.</summary>
        public Task ResetAsync() => _respawner.ResetAsync(_connection);

        public async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
            await _container.DisposeAsync();
        }
    }

    [CollectionDefinition(Name)]
    public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
    {
        public const string Name = "postgres";
    }
}
