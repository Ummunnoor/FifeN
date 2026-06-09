using Domain.Entities.Catalog;
using Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(80).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            b.HasIndex(x => x.Slug).IsUnique();
        }
    }

    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(80).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            b.Property(x => x.Condition).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.PriceType).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Attributes).HasColumnType("jsonb");

            b.OwnsOne(x => x.Price, m =>
            {
                m.Property(p => p.Amount).HasColumnName("PriceAmount").HasColumnType("numeric(18,2)");
                m.Property(p => p.Currency).HasColumnName("PriceCurrency").HasMaxLength(3);
            });
            b.Navigation(x => x.Price).IsRequired();

            b.OwnsOne(x => x.Location, l =>
            {
                l.Property(p => p.State).HasColumnName("State").HasConversion<string>().HasMaxLength(20);
                l.Property(p => p.City).HasColumnName("City").HasMaxLength(80);
            });
            b.Navigation(x => x.Location).IsRequired();

            b.HasOne(x => x.Vendor)
                .WithMany(v => v.Products)
                .HasForeignKey(x => x.VendorProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.Images)
                .WithOne()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Postgres-native full-text search: generated tsvector + GIN index (no external engine).
            b.HasGeneratedTsVectorColumn(p => p.SearchVector!, "english", p => new { p.Title, p.Description })
                .HasIndex(p => p.SearchVector!)
                .HasMethod("GIN");

            // Hot read path: active listings within a category, newest first.
            b.HasIndex(x => new { x.CategoryId, x.CreatedAtUtc })
                .HasFilter($"\"Status\" = '{nameof(ListingStatus.Live)}'");
            b.HasIndex(x => x.VendorProfileId);
        }
    }

    public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.CloudinaryPublicId).IsRequired();
            b.Property(x => x.Url).IsRequired();
            b.HasIndex(x => x.ProductId);
        }
    }
}
