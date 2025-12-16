using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;  
namespace Persistence
{
    public class FifeNDbContextFactory : IDesignTimeDbContextFactory<FifeNDbContext>
        {
            public FifeNDbContext CreateDbContext(string[] args)
            {
                // Load appsettings.json from the API project
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "API"))
                    .AddJsonFile("appsettings.json")
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                var optionsBuilder = new DbContextOptionsBuilder<FifeNDbContext>();
                optionsBuilder.UseNpgsql(connectionString);

                return new FifeNDbContext(optionsBuilder.Options);
            }
        }
}
