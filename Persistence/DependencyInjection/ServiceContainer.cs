using System.Text;
using Application.Services.Interfaces;
using Application.Services.Interfaces.Logging;
using Domain.Entities;
using Domain.Entities.Identity;
using Domain.Entities.Product;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Persistence.Middleware;
using Persistence.Repositories;
using Persistence.Services;

namespace Persistence.DependencyInjection
{
    public  static class ServiceContainer
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
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
            services.AddScoped(typeof(IAppLogger<>), typeof(SerilogLoggerAdapter<>));

           services.AddDefaultIdentity<User>(options =>
           {
               options.SignIn.RequireConfirmedEmail = true;
               options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;
           })
           .AddRoles<IdentityRole>()
           .AddEntityFrameworkStores<FifeNDbContext>();
            
            services.AddAuthentication(options =>{
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = configuration["JWT:Audience"],
                    ValidIssuer = configuration["JWT:Issuer"],
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]!))
                };
            }
                
                );
            return services;
        }

        public static IApplicationBuilder UsePersistenceServices(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            return app;
        }
    }
}