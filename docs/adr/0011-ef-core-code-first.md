# ADR-0011: EF Core Code-First Migrations

## Status
Accepted

## Context
Database schema management requirements:
- Version-controlled schema changes
- Repeatable deployments across environments
- Rollback capability
- Developer-friendly workflow
- Alignment with FRD-provided baseline SQL

## Decision
Use **EF Core Code-First Migrations** as the primary schema management approach.

### Migration Strategy

1. **Initial Migration**: Scaffold from FRD baseline SQL (Anexo A)
2. **Subsequent Migrations**: Code-first from entity changes
3. **Deployment**: `dotnet ef database update` at container startup (or CI/CD step)

### Initial Migration Creation

```bash
# 1. Create entities matching FRD schema
# 2. Configure in OnModelCreating / IEntityTypeConfiguration
# 3. Scaffold initial migration
dotnet ef migrations add InitialCreate \
  --project src/backend/src/BookingPlatform.Infrastructure \
  --startup-project src/backend/src/BookingPlatform.Api \
  --output-dir Persistence/Migrations
```

### Entity Configuration Example

```csharp
// BookingPlatform.Infrastructure/Persistence/Configurations/ResourceConfiguration.cs
public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("resources", "booking");
        
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();
        
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Capacity).IsRequired();
        builder.Property(r => r.PublicQrId).IsRequired(false);
        builder.Property(r => r.QrVersion).IsRequired().HasDefaultValue(1);
        
        // QR Policy Check Constraint
        builder.HasCheckConstraint(
            "ck_resource_qr_policy",
            "(resource_type_code IN ('OPEN_WORKSPACE','CLOSED_OFFICE') AND public_qr_id IS NOT NULL) " +
            "OR (resource_type_code = 'MEETING_ROOM' AND public_qr_id IS NULL)"
        );
        
        // Relationships
        builder.HasOne(r => r.ResourceType)
               .WithMany()
               .HasForeignKey(r => r.ResourceTypeCode)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(r => r.Location)
               .WithMany()
               .HasForeignKey(r => r.LocationId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(r => r.Floor)
               .WithMany()
               .HasForeignKey(r => r.FloorId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(r => r.Zone)
               .WithMany()
               .HasForeignKey(r => r.ZoneId)
               .OnDelete(DeleteBehavior.SetNull);
        
        // Indexes
        builder.HasIndex(r => r.ResourceTypeCode);
        builder.HasIndex(r => r.FloorId);
        builder.HasIndex(r => new { r.Active, r.Reservable });
    }
}
```

### Exclusion Constraints (Raw SQL in Migration)

```csharp
// In Migration Up()
migrationBuilder.Sql(@"
    ALTER TABLE booking.reservations 
    DROP CONSTRAINT IF EXISTS ex_no_resource_overlap;
    
    ALTER TABLE booking.reservations 
    ADD CONSTRAINT ex_no_resource_overlap
    EXCLUDE USING gist (
        resource_id WITH =,
        tsrange(reservation_date + start_time, reservation_date + end_time, '[)') WITH &&
    )
    WHERE (status IN ('CONFIRMED', 'CHECKED_IN'));
");
```

### DbContext Configuration

```csharp
// BookingPlatformDbContext.cs
public class BookingPlatformDbContext : DbContext
{
    public BookingPlatformDbContext(DbContextOptions<BookingPlatformDbContext> options)
        : base(options) { }

    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    // ... other DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("booking");
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.HasPostgresExtension("citext");

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingPlatformDbContext).Assembly);

        // Enum mappings
        modelBuilder.Entity<Reservation>()
            .Property(r => r.Status)
            .HasConversion<string>();

        // Value converters, global query filters, etc.
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Update timestamps, domain events, etc.
        return await base.SaveChangesAsync(ct);
    }
}
```

### Deployment

```yaml
# GitHub Actions - Deploy step
- name: Run Migrations
  run: |
    dotnet tool install --global dotnet-ef
    dotnet ef database update \
      --project src/backend/src/BookingPlatform.Infrastructure \
      --startup-project src/backend/src/BookingPlatform.Api \
      --connection "${{ secrets.DB_CONNECTION_STRING }}"
```

## Consequences

### Positive
- **Version Control**: Migrations in git, reviewable, auditable
- **Type Safety**: Schema changes compile-checked
- **Rollback**: `dotnet ef database update <PreviousMigration>`
- **Developer Workflow**: `add-migration` → review → commit → deploy
- **EF Core Features**: Global query filters, value converters, owned types
- **Baseline Alignment**: Initial migration matches FRD SQL exactly

### Negative
- **Complex Migrations**: Exclusion constraints, triggers require raw SQL
- **Drift Risk**: Manual DB changes not tracked (policy: no manual changes)
- **Locking**: Long-running migrations can lock tables (plan maintenance windows)
- **Learning Curve**: Team must understand EF Core patterns

### Neutral
- Requires `Microsoft.EntityFrameworkCore.Design` and `Tools` packages
- Migration files should be reviewed like code (not auto-generated blindly)

## Alternatives Considered

1. **Database-First (Reverse Engineering)**
   - Rejected: Schema changes in DB, not code; harder to review

2. **Raw SQL Migration Files (Flyway / DbUp)**
   - Rejected: Lose EF Core integration, no compile-time checking

3. **Schema Comparison Tools (Redgate / SSDT)**
   - Rejected: Cost, Windows-centric, not code-first

## References
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Raw SQL in Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli#raw-sql)
- [PostgreSQL Provider](https://www.npgsql.org/efcore/)