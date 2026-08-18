using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorUserId).IsRequired(false);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntityId).IsRequired(false);
        builder.Property(x => x.BeforeValue).IsRequired(false).HasColumnType("jsonb");
        builder.Property(x => x.AfterValue).IsRequired(false).HasColumnType("jsonb");
        builder.Property(x => x.Reason).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.IpAddress).IsRequired(false).HasColumnType("inet");
        builder.Property(x => x.UserAgent).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.CorrelationId).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.ActorUser)
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.ActorUserId, x.CreatedAt }).IsDescending().HasDatabaseName("ix_audit_logs_actor");
        builder.HasIndex(x => new { x.EntityName, x.EntityId }).HasDatabaseName("ix_audit_logs_entity");
        builder.HasIndex(x => new { x.Action, x.CreatedAt }).IsDescending().HasDatabaseName("ix_audit_logs_action");
        builder.HasIndex(x => x.CorrelationId).HasDatabaseName("ix_audit_logs_correlation");
    }
}