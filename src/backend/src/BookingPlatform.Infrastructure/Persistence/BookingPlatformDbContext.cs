using Microsoft.EntityFrameworkCore;
using BookingPlatform.Domain.Entities;
using BookingPlatform.Domain.Enums;
using BookingPlatform.Infrastructure.Persistence.Configurations;

namespace BookingPlatform.Infrastructure.Persistence;

public class BookingPlatformDbContext : DbContext
{
    public BookingPlatformDbContext(DbContextOptions<BookingPlatformDbContext> options)
        : base(options) { }

    // DbSets
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<ResourceType> ResourceTypes => Set<ResourceType>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();
    public DbSet<ApplicationRole> ApplicationRoles => Set<ApplicationRole>();
    public DbSet<UserBusinessProfile> UserBusinessProfiles => Set<UserBusinessProfile>();
    public DbSet<UserApplicationRole> UserApplicationRoles => Set<UserApplicationRole>();
    public DbSet<ResourceAccessPolicy> ResourceAccessPolicies => Set<ResourceAccessPolicy>();
    public DbSet<ReservationException> ReservationExceptions => Set<ReservationException>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();
    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("booking");
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.HasPostgresExtension("citext");

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingPlatformDbContext).Assembly);

        // Enum configurations
        modelBuilder.Entity<Reservation>()
            .Property(r => r.Status)
            .HasConversion<string>();

        modelBuilder.Entity<NotificationOutbox>()
            .Property(n => n.Status)
            .HasConversion<string>();

        modelBuilder.Entity<NotificationOutbox>()
            .Property(n => n.Type)
            .HasConversion<string>();

        modelBuilder.Entity<CheckIn>()
            .Property(c => c.Method)
            .HasConversion<string>();

        // Global query filters for soft delete pattern (if needed)
        // modelBuilder.Entity<AppUser>().HasQueryFilter(u => u.Active);
        // modelBuilder.Entity<Resource>().HasQueryFilter(r => r.Active);

        // Indexes and constraints beyond what's in configurations
        ConfigureIndexes(modelBuilder);
        ConfigureExclusionConstraints(modelBuilder);
        ConfigureCheckConstraints(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Reservations indexes
        modelBuilder.Entity<Reservation>()
            .HasIndex(r => new { r.UserId, r.ReservationDate })
            .HasDatabaseName("ix_reservations_user_date");

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => new { r.ResourceId, r.ReservationDate })
            .HasDatabaseName("ix_reservations_resource_date");

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => r.Status)
            .HasDatabaseName("ix_reservations_status");

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => new { r.UserId, r.ReservationDate, r.Status })
            .HasFilter("status IN ('CONFIRMED', 'CHECKED_IN')")
            .HasDatabaseName("ix_reservations_future_active");

        // CheckIns indexes
        modelBuilder.Entity<CheckIn>()
            .HasIndex(c => new { c.UserId, c.CheckedInAt })
            .HasDatabaseName("ix_checkins_user");

        modelBuilder.Entity<CheckIn>()
            .HasIndex(c => new { c.ResourceId, c.CheckedInAt })
            .HasDatabaseName("ix_checkins_resource");

        // Notifications indexes
        modelBuilder.Entity<NotificationOutbox>()
            .HasIndex(n => new { n.Status, n.ScheduledAt })
            .HasFilter("status = 'PENDING'")
            .HasDatabaseName("ix_notification_pending");

        // Audit Logs indexes
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.ActorUserId, a.CreatedAt })
            .HasDatabaseName("ix_audit_logs_actor");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.EntityName, a.EntityId })
            .HasDatabaseName("ix_audit_logs_entity");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.Action, a.CreatedAt })
            .HasDatabaseName("ix_audit_logs_action");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.CorrelationId)
            .HasDatabaseName("ix_audit_logs_correlation");
    }

    private void ConfigureExclusionConstraints(ModelBuilder modelBuilder)
    {
        // Resource overlap exclusion constraint
        modelBuilder.Entity<Reservation>()
            .HasNoKey() // Raw SQL for exclusion constraint
            .ToTable("reservations", "booking");

        // These will be created via migration raw SQL
    }

    private void ConfigureCheckConstraints(ModelBuilder modelBuilder)
    {
        // AppSettings singleton
        modelBuilder.Entity<AppSettings>()
            .HasIndex(a => true)
            .IsUnique()
            .HasDatabaseName("ux_app_settings_singleton");

        // UserBusinessProfile unique active
        modelBuilder.Entity<UserBusinessProfile>()
            .HasIndex(u => new { u.UserId, u.ProfileCode })
            .IsUnique()
            .HasFilter("active = true")
            .HasDatabaseName("ux_user_profile_active");

        // UserApplicationRole unique active
        modelBuilder.Entity<UserApplicationRole>()
            .HasIndex(u => new { u.UserId, u.RoleCode })
            .IsUnique()
            .HasFilter("active = true")
            .HasDatabaseName("ux_user_role_active");

        // ResourceAccessPolicy unique
        modelBuilder.Entity<ResourceAccessPolicy>()
            .HasIndex(r => new { r.ResourceTypeCode, r.BusinessProfileCode })
            .IsUnique()
            .HasDatabaseName("ux_resource_access_policy");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                auditable.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}