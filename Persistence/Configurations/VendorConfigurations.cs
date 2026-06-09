using Domain.Entities.Vendors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class VendorProfileConfiguration : IEntityTypeConfiguration<VendorProfile>
    {
        public void Configure(EntityTypeBuilder<VendorProfile> b)
        {
            b.HasKey(x => x.Id);

            // Case-insensitive unique business name via the citext extension.
            b.Property(x => x.BusinessName).HasColumnType("citext").IsRequired();
            b.HasIndex(x => x.BusinessName).IsUnique();

            b.Property(x => x.WhatsAppNumber).HasMaxLength(20).IsRequired();
            b.Property(x => x.VerificationMethod).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.TrustTier).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.VerifiedName).HasMaxLength(160);
            b.Property(x => x.NameMatchConfidence).HasColumnType("numeric(5,4)");
            b.Property(x => x.Version).IsRowVersion(); // xmin optimistic concurrency

            b.HasOne(x => x.User)
                .WithOne(u => u.VendorProfile)
                .HasForeignKey<VendorProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.UserId).IsUnique();
        }
    }

    public class VendorRequestConfiguration : IEntityTypeConfiguration<VendorRequest>
    {
        public void Configure(EntityTypeBuilder<VendorRequest> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.BusinessName).HasMaxLength(160).IsRequired();
            b.Property(x => x.WhatsAppNumber).HasMaxLength(20).IsRequired();
            b.Property(x => x.VerificationMethod).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.RejectionReason).HasConversion<string>().HasMaxLength(40);
            b.Property(x => x.Version).IsRowVersion();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.UserId);
        }
    }
}
