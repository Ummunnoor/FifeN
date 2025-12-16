using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Entities.Product;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repositories;

namespace Persistence.DependencyInjection
{
    public  static class ServiceContainer
    {
        public static IServiceCollection AddInfrastructureService(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = "DefaultConnection";
            services.AddDbContext<FifeNDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString(connectionString),
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(FifeNDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure();
            }),
            ServiceLifetime.Scoped);
            services.AddScoped<IGeneric<Product>, GenericRepository<Product>>();
            services.AddScoped<IGeneric<Category>, GenericRepository<Category>>();
            services.AddScoped<IGeneric<ProductAttribute>, GenericRepository<ProductAttribute>>();
            return services;
        }
    }
}