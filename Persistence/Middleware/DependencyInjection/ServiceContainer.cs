using System;
using System.Text;
using Application.Abstractions;
using Application.Services.Interfaces;
using CloudinaryDotNet;
using Application.Services.Interfaces.Logging;
using Application.Modules.Identity.Services.Interfaces;
using Application.Modules.Admin.Services.Interfaces;
using Application.Modules.Catalog.Services.Interfaces;
using Application.Modules.Discovery.Services.Interfaces;
using Application.Modules.Engagement.Services.Interfaces;
using Application.Modules.Notifications.Services.Interfaces;
using Application.Modules.TrustSafety.Services.Interfaces;
using Application.Modules.Vendors.Services.Interfaces;
using Persistence.Modules.Admin;
using Persistence.Modules.Catalog;
using Persistence.Modules.Discovery;
using Persistence.Modules.Engagement;
using Persistence.Modules.Notifications;
using Persistence.Modules.Shared;
using Persistence.Modules.TrustSafety;
using Persistence.Modules.Vendors;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Persistence.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Persistence.Modules.Identity;
using Persistence.Repositories;
using Persistence.Services;

namespace Persistence.DependencyInjection
{
    /// <summary>
    /// Registers the persistence layer: the DbContext, ASP.NET Identity (Guid-keyed), JWT bearer
    /// authentication, authorization policies, the generic repository, and the infrastructure adapters
    /// that implement the application's ports.
    /// </summary>
    public static class ServiceContainer
    {
        public static IServiceCollection AddPersistenceServices(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<FifeNDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly(typeof(FifeNDbContext).Assembly.FullName);
                        npgsql.EnableRetryOnFailure();
                        // Product.Attributes / Report.MetadataJson are POCO collections persisted as
                        // jsonb; dynamic JSON serialization must be opted into explicitly (Npgsql 8+).
                        npgsql.ConfigureDataSource(dataSource => dataSource.EnableDynamicJson());
                    }));

            // Generic repository (open generic).
            services.AddScoped(typeof(IGeneric<>), typeof(GenericRepository<>));
            services.AddScoped(typeof(IAppLogger<>), typeof(SerilogLoggerAdapter<>));

            AddIdentity(services);
            AddJwtAuthentication(services, configuration);
            AddAuthorizationPolicies(services);

            // DB-backed handler for the RequireVendor policy's verified-status check.
            services.AddScoped<IAuthorizationHandler, VerifiedVendorHandler>();

            // Application port adapters (Identity module).
            services.AddSingleton<ISecureHasher, Sha256SecureHasher>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
            services.AddScoped<IUserAccountStore, UserAccountStore>();
            services.AddScoped<IPhoneVerificationStore, PhoneVerificationStore>();
            services.AddScoped<IMfaStore, MfaStore>();
            services.AddScoped<IOtpSender, LoggingOtpSender>();

            // Vendors module + shared cross-cutting adapters.
            services.AddScoped<IVendorRepository, VendorRepository>();
            services.AddScoped<IUserAdminStore, UserAdminStore>();
            services.AddScoped<IIdentityVerificationService, DevIdentityVerificationService>();
            services.AddScoped<IAuditLogger, AuditLogger>();
            services.AddScoped<INotificationService, NotificationService>();

            // Catalog module.
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            AddImageStorage(services, configuration);

            // Discovery module.
            services.AddScoped<IProductQueryRepository, ProductQueryRepository>();

            // Engagement module.
            services.AddScoped<IInteractionRepository, InteractionRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();

            // Trust & safety module.
            services.AddScoped<IReportRepository, ReportRepository>();

            // Notifications module.
            services.AddScoped<INotificationRepository, NotificationRepository>();

            // Admin module.
            services.AddScoped<IDashboardRepository, DashboardRepository>();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }

        private static void AddIdentity(IServiceCollection services)
        {
            services.AddIdentityCore<User>(options =>
                {
                    // Phone + OTP is the only credential; passwords and email confirmation are unused.
                    options.SignIn.RequireConfirmedEmail = false;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                    options.User.RequireUniqueEmail = false;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<FifeNDbContext>()
                .AddDefaultTokenProviders();
        }

        private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            var key = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT signing key (Jwt:Key) is not configured.");

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        RequireExpirationTime = true,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                    };
                });
        }

        /// <summary>
        /// Uses the real Cloudinary adapter when credentials are configured (the <c>Cloudinary</c>
        /// section), otherwise falls back to the placeholder dev store so local runs work without an
        /// account.
        /// </summary>
        private static void AddImageStorage(IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(CloudinaryOptions.SectionName).Get<CloudinaryOptions>()
                ?? new CloudinaryOptions();

            if (options.IsConfigured)
            {
                services.AddSingleton(new Cloudinary(
                    new Account(options.CloudName, options.ApiKey, options.ApiSecret)));
                services.AddScoped<IImageStorageService, CloudinaryImageStorageService>();
            }
            else
            {
                services.AddScoped<IImageStorageService, DevImageStorageService>();
            }
        }

        private static void AddAuthorizationPolicies(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireVendor", p => p
                    .RequireRole(nameof(AppRole.Vendor))
                    .AddRequirements(new VerifiedVendorRequirement()));
                // Admin actions require the role AND a satisfied second factor (amr=mfa), per spec §4.1.
                // The MFA enroll/verify endpoints themselves authorize on role alone (see AuthController)
                // so an admin can bootstrap MFA before this gate applies.
                options.AddPolicy("RequireAdmin", p => p
                    .RequireRole(nameof(AppRole.Admin))
                    .RequireClaim("amr", "mfa"));
                options.AddPolicy("RequireOwner", p => p.RequireClaim("is_owner", "true"));
            });
        }
    }
}
