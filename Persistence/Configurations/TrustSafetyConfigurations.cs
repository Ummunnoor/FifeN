using Domain.Entities.Notifications;
using Domain.Entities.TrustSafety;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class ReportConfiguration : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.TargetType).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Reason).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Note).HasMaxLength(500);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => new { x.TargetType, x.TargetId });
        }
    }

    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Action).HasMaxLength(100).IsRequired();
            b.Property(x => x.ObjectType).HasMaxLength(80).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500);
            b.Property(x => x.MetadataJson).HasColumnType("jsonb");
            b.Property(x => x.IpAddress).HasMaxLength(64);
            b.HasIndex(x => new { x.ObjectType, x.ObjectId });
            b.HasIndex(x => x.ActorUserId);
            // Append-only: UPDATE/DELETE blocked at the repository layer (and by a DB rule in prod).
        }
    }

    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            b.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Title).HasMaxLength(160).IsRequired();
            b.Property(x => x.Body).HasMaxLength(1000).IsRequired();
            b.HasIndex(x => new { x.RecipientUserId, x.IsRead });
        }
    }
}
