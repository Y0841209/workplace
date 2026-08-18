using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkplaceBooking.Application;
using WorkplaceBooking.Infrastructure.Persistence;
using WorkplaceBooking.Infrastructure.Persistence.Repositories;
using WorkplaceBooking.Infrastructure.Services;

namespace WorkplaceBooking.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication(); // Add Application layer first

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
        services.AddScoped<IRepository<ResourceType>, EfRepository<ResourceType>>();
        services.AddScoped<IRepository<Location>, EfRepository<Location>>();
        services.AddScoped<IRepository<Floor>, EfRepository<Floor>>();
        services.AddScoped<IRepository<Zone>, EfRepository<Zone>>();

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Infrastructure Services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IReservationPolicyService, ReservationPolicyService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IQrValidationService, QrValidationService>();
        services.AddScoped<IUserAuthorizationService, UserAuthorizationService>();

        // Options
        services.Configure<EmailSettings>(configuration.GetSection("Email"));

        return services;
    }
}