using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkplaceBooking.Application.Common.Interfaces;
using AppIUserAuth = WorkplaceBooking.Application.Common.Interfaces.IUserAuthorizationService;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Infrastructure.Persistence;
using WorkplaceBooking.Infrastructure.Persistence.Repositories;
using WorkplaceBooking.Infrastructure.Services;
using WorkplaceBooking.SharedKernel.Primitives;

namespace WorkplaceBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IRepository<Reservation>, ReservationRepository>();
        services.AddScoped<IRepository<Resource>, ResourceRepository>();
        services.AddScoped<IRepository<AppUser>, UserRepository>();
        services.AddScoped<IRepository<ResourceType>, EfRepository<ResourceType>>();
        services.AddScoped<IRepository<Location>, EfRepository<Location>>();
        services.AddScoped<IRepository<Floor>, EfRepository<Floor>>();
        services.AddScoped<IRepository<Zone>, EfRepository<Zone>>();
        services.AddScoped<IRepository<BusinessProfile>, EfRepository<BusinessProfile>>();
        services.AddScoped<IRepository<ApplicationRole>, EfRepository<ApplicationRole>>();
        services.AddScoped<IRepository<UserBusinessProfile>, EfRepository<UserBusinessProfile>>();
        services.AddScoped<IRepository<UserApplicationRole>, EfRepository<UserApplicationRole>>();
        services.AddScoped<IRepository<ResourceAccessPolicy>, EfRepository<ResourceAccessPolicy>>();
        services.AddScoped<IRepository<ReservationException>, EfRepository<ReservationException>>();
        services.AddScoped<IRepository<CheckIn>, EfRepository<CheckIn>>();
        services.AddScoped<IRepository<NotificationOutbox>, EfRepository<NotificationOutbox>>();
        services.AddScoped<IRepository<AuditLog>, EfRepository<AuditLog>>();
        services.AddScoped<IRepository<AppSettings>, EfRepository<AppSettings>>();

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Infrastructure Services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IReservationPolicyService, ReservationPolicyService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IQrValidationService, QrValidationService>();
        services.AddScoped<AppIUserAuth, UserAuthorizationService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Options
        services.Configure<EmailSettings>(configuration.GetSection("Email"));

        return services;
    }
}

// Generic EF Repository
public class EfRepository<T> : IRepository<T> where T : Entity
{
    private readonly AppDbContext _context;

    public EfRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.Default.GetQuery(_context.Set<T>().AsQueryable(), spec);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.Default.GetQuery(_context.Set<T>().AsQueryable(), spec);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.Default.GetQuery(_context.Set<T>().AsQueryable(), spec);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.Default.GetQuery(_context.Set<T>().AsQueryable(), spec);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _context.Set<T>().AddAsync(entity, cancellationToken);
    }

    public void Update(T entity)
    {
        _context.Set<T>().Update(entity);
    }

    public void Delete(T entity)
    {
        _context.Set<T>().Remove(entity);
    }
}