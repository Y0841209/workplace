using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using WorkplaceBooking.Application.Common.Behaviors;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Application.Common.Mappings;
using WorkplaceBooking.Application.Features.Resources.Mappings;
using WorkplaceBooking.Application.Features.Reservations.Mappings;
using WorkplaceBooking.Application.Features.CheckIns.Mappings;
using WorkplaceBooking.Application.Validators;
using WorkplaceBooking.Application.Features.Reservations.Validators;

namespace WorkplaceBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
            cfg.AddProfile<ResourceMappingProfile>();
            cfg.AddProfile<ReservationMappingProfile>();
            cfg.AddProfile<CheckInMappingProfile>();
        });

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<CreateResourceValidator>();
        services.AddValidatorsFromAssemblyContaining<WorkplaceBooking.Application.Features.Reservations.Validators.CreateReservationValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateResourceDtoValidator>();

        // Pipeline Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

        return services;
    }
}