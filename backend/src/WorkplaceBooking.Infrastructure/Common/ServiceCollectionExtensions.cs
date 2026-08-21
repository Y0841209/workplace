using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace WorkplaceBooking.Infrastructure.Common;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}