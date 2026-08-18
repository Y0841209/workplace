using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Application.Interfaces;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.UseCases.Queries.Reservations;

public class GetAvailabilityHandler : IRequestHandler<GetAvailabilityQuery, Result<IReadOnlyList<AvailabilitySlotDto>>>
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

    public async Task<Result<IReadOnlyList<AvailabilitySlotDto>>> Handle(GetAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var spec = new AvailableResourcesSpec(
            request.Date,
            request.StartTime,
            request.EndTime,
            request.ResourceTypeCode,
            request.FloorId,
            request.ZoneId,
            request.MinCapacity);

        var resources = await _resourceRepository.ListAsync(spec, CancellationToken.None);
        var slots = new List<AvailabilitySlotDto>();

        foreach (var resource in resources)
        {
            var isAvailable = await _availabilityService.IsAvailableAsync(
                resource.Id,
                request.Date,
                request.StartTime,
                request.EndTime,
                CancellationToken.None);

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

        return Result.Success(slots);
    }

    private async Task<string> GetFloorName(Guid floorId, CancellationToken ct)
    {
        // Would need FloorRepository - simplified
        return "Piso";
    }

    private async Task<string?> GetZoneName(Guid zoneId, CancellationToken ct)
    {
        // Would need ZoneRepository - simplified
        return null;
    }
}