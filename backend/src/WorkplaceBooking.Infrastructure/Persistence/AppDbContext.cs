using Microsoft.EntityFrameworkCore;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Infrastructure.Persistence.Configurations;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.SharedKernel.Primitives;

namespace WorkplaceBooking.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

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

        // Exclusion constraints for preventing double booking
        // These are created via raw SQL in migrations
        base.OnModelCreating(modelBuilder);
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

        // Capture domain events
        var entitiesWithEvents = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var entityEntry in entitiesWithEvents)
        {
            entityEntry.Entity.ClearDomainEvents();
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        // Domain events would be dispatched here
        // For now, they are cleared after save

        return result;
    }
}

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}