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

            b.Property(x => x.Email).HasMaxLength(320);
            b.Property(x => x.NormalizedEmail).HasMaxLength(320);
            b.Property(x => x.GoogleSubjectId).HasMaxLength(100);
            b.HasIndex(x => x.NormalizedEmail).IsUnique();
            b.HasIndex(x => x.GoogleSubjectId).IsUnique().HasFilter("\"GoogleSubjectId\" IS NOT NULL");

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
            b.Property(x => x.RevokedAtUtc).IsConcurrencyToken();
            b.HasIndex(x => x.TokenHash);
            b.HasIndex(x => x.UserId);
        }
    }

    public class EmailVerificationConfiguration : IEntityTypeConfiguration<EmailVerification>
    {
        public void Configure(EntityTypeBuilder<EmailVerification> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Email).HasMaxLength(320).IsRequired();
            b.Property(x => x.CodeHash).IsRequired();
            b.HasIndex(x => new { x.Email, x.IsConsumed, x.CreatedAtUtc });
        }
    }
}
