using Domain.Entities.Catalog;
using Domain.Entities.Identity;
using Domain.Entities.Interactions;
using Domain.Entities.Notifications;
using Domain.Entities.Reviews;
using Domain.Entities.TrustSafety;
using Domain.Entities.Vendors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Persistence
{
    /// <summary>
    /// The single application <see cref="DbContext"/> for the TradeNaija modular monolith. Identity is
    /// keyed by <see cref="System.Guid"/>; entity mappings live in <c>Persistence/Configurations</c> and
    /// are applied by assembly scan.
    /// </summary>
    public class FifeNDbContext(DbContextOptions<FifeNDbContext> options)
        : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
    {
        // Identity & access
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<PhoneVerification> PhoneVerifications => Set<PhoneVerification>();

        // Vendors & verification
        public DbSet<VendorProfile> VendorProfiles => Set<VendorProfile>();
        public DbSet<VendorRequest> VendorRequests => Set<VendorRequest>();

        // Catalog
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();

        // Engagement
        public DbSet<Interaction> Interactions => Set<Interaction>();
        public DbSet<Review> Reviews => Set<Review>();

        // Trust & safety
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        // Notifications
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // citext powers case-insensitive unique business names.
            modelBuilder.HasPostgresExtension("citext");

            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FifeNDbContext).Assembly);
        }
    }
}
