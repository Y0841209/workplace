using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Features.Resources.DTOs;
using WorkplaceBooking.Application.Features.Resources.Queries;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Resources.Handlers;

public class GetAvailabilityHandler : IRequestHandler<GetAvailabilityQuery, Ardalis.Result.Result<IReadOnlyList<AvailabilitySlotDto>>>
{
    private readonly IAvailabilityService _availabilityService;
    private readonly IRepository<Resource> _resourceRepository;

    public GetAvailabilityHandler(
        IAvailabilityService availabilityService,
        IRepository<Resource> resourceRepository)
    {
        _availabilityService = availabilityService;
        _resourceRepository = resourceRepository;
    }

    public async Task<Ardalis.Result.Result<IReadOnlyList<AvailabilitySlotDto>>> Handle(GetAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var spec = new AvailableResourcesSpec(
            request.Date,
            request.StartTime,
            request.EndTime,
            request.ResourceTypeCode,
            request.FloorId,
            request.ZoneId,
            request.MinCapacity);

        var resources = await _resourceRepository.ListAsync(spec, cancellationToken);
        var slots = new List<AvailabilitySlotDto>();

        foreach (var resource in resources)
        {
            var isAvailable = await _availabilityService.IsAvailableAsync(
                resource.Id,
                request.Date,
                request.StartTime,
                request.EndTime,
                cancellationToken);

            if (isAvailable)
            {
                var floor = await GetFloorName(resource.FloorId, CancellationToken.None);
                var zone = resource.ZoneId.HasValue
                    ? await GetZoneName(resource.ZoneId.Value, CancellationToken.None)
                    : null;

                slots.Add(new AvailabilitySlotDto(
                    resource.Id,
                    resource.Code,
                    resource.Name,
                    resource.ResourceTypeCode,
                    resource.FloorId,
                    floor,
                    resource.ZoneId,
                    zone,
                    resource.Capacity,
                    request.StartTime,
                    request.EndTime,
                    true));
            }
        }

        return Ardalis.Result.Result.Success<IReadOnlyList<AvailabilitySlotDto>>(slots);
    }

    private Task<string> GetFloorName(Guid floorId, CancellationToken ct)
    {
        // Would need FloorRepository - simplified
        return Task.FromResult("Piso");
    }

    private Task<string?> GetZoneName(Guid zoneId, CancellationToken ct)
    {
        // Would need ZoneRepository - simplified
        return Task.FromResult<string?>(null);
    }
}