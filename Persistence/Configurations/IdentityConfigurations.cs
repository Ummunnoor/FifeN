using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> b)
        {
            b.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            b.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            b.Property(x => x.SecondaryPhoneNumber).HasMaxLength(20);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            b.Ignore(x => x.DisplayName);

            // One account per primary phone.
            b.HasIndex(x => x.PhoneNumber).IsUnique();

            b.HasMany(x => x.RefreshTokens)
                .WithOne()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.TokenHash).IsRequired();
            b.Ignore(x => x.IsActive);
            b.HasIndex(x => x.TokenHash);
            b.HasIndex(x => x.UserId);
        }
    }

    public class PhoneVerificationConfiguration : IEntityTypeConfiguration<PhoneVerification>
    {
        public void Configure(EntityTypeBuilder<PhoneVerification> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
            b.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.CodeHash).IsRequired();
            b.HasIndex(x => new { x.PhoneNumber, x.Status });
        }
    }
}
