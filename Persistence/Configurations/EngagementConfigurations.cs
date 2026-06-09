using Domain.Entities.Interactions;
using Domain.Entities.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class InteractionConfiguration : IEntityTypeConfiguration<Interaction>
    {
        public void Configure(EntityTypeBuilder<Interaction> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.LeadStatus).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.BuyerMessage).HasMaxLength(500);
            b.Property(x => x.OfferPrice).HasColumnType("numeric(18,2)");

            // Cross-discovery lookups: has this buyer contacted this vendor before?
            b.HasIndex(x => new { x.BuyerUserId, x.VendorProfileId });
            // Vendor leads dashboard.
            b.HasIndex(x => new { x.VendorProfileId, x.LeadStatus });
            b.HasIndex(x => x.ProductId);
        }
    }

    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Text).HasMaxLength(500);

            // One review per buyer per product.
            b.HasIndex(x => new { x.AuthorUserId, x.ProductId }).IsUnique();
            b.HasIndex(x => x.VendorProfileId);

            // Public reads see visible reviews only; admin queries opt out via IgnoreQueryFilters().
            b.HasQueryFilter(r => r.Status == Domain.Entities.Enums.ReviewStatus.Visible);
        }
    }
}
