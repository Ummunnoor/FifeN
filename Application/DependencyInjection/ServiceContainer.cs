using Application.Modules.Admin.Services.Implementations;
using Application.Modules.Admin.Services.Interfaces;
using Application.Modules.Catalog.Services.Implementations;
using Application.Modules.Catalog.Services.Interfaces;
using Application.Modules.Discovery.Services.Implementations;
using Application.Modules.Discovery.Services.Interfaces;
using Application.Modules.Engagement.Services.Implementations;
using Application.Modules.Engagement.Services.Interfaces;
using Application.Modules.Identity.Services.Implementations;
using Application.Modules.Identity.Services.Interfaces;
using Application.Modules.Notifications.Services.Implementations;
using Application.Modules.Notifications.Services.Interfaces;
using Application.Modules.TrustSafety.Services.Implementations;
using Application.Modules.TrustSafety.Services.Interfaces;
using Application.Modules.Vendors.Services.Implementations;
using Application.Modules.Vendors.Services.Interfaces;
using Application.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace Application.DependencyInjection
{
    /// <summary>
    /// Registers application-layer services. Infrastructure ports (token issuance, stores, OTP delivery)
    /// are implemented and registered in the persistence layer.
    /// </summary>
    public static class ServiceContainer
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<ServiceContainerMarker>();
            services.AddScoped<IValidationService, ValidationService>();

            // Identity module
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IMfaService, MfaService>();

            // Vendors module
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<IVendorAdminService, VendorAdminService>();

            // Catalog module
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IProductAdminService, ProductAdminService>();

            // Discovery module
            services.AddScoped<IDiscoveryService, DiscoveryService>();

            // Engagement module
            services.AddScoped<IInteractionService, InteractionService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IReviewAdminService, ReviewAdminService>();

            // Trust & safety module
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IReportAdminService, ReportAdminService>();

            // Notifications module
            services.AddScoped<INotificationFeedService, NotificationFeedService>();

            // Admin module
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();

            return services;
        }
    }

    /// <summary>Assembly marker used to discover FluentValidation validators in this project.</summary>
    public sealed class ServiceContainerMarker;
}
