using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Features.Resources.DTOs;
using WorkplaceBooking.Application.Features.Resources.Queries;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Resources.Handlers;

public class GetMeetingRoomsHandler : IRequestHandler<GetMeetingRoomsQuery, Ardalis.Result.Result<IReadOnlyList<ResourceDto>>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<ResourceType> _resourceTypeRepository;
    private readonly IRepository<Location> _locationRepository;
    private readonly IRepository<Floor> _floorRepository;
    private readonly IRepository<Zone> _zoneRepository;

    public GetMeetingRoomsHandler(
        IRepository<Resource> resourceRepository,
        IRepository<ResourceType> resourceTypeRepository,
        IRepository<Location> locationRepository,
        IRepository<Floor> floorRepository,
        IRepository<Zone> zoneRepository)
    {
        _resourceRepository = resourceRepository;
        _resourceTypeRepository = resourceTypeRepository;
        _locationRepository = locationRepository;
        _floorRepository = floorRepository;
        _zoneRepository = zoneRepository;
    }

    public async Task<Ardalis.Result.Result<IReadOnlyList<ResourceDto>>> Handle(GetMeetingRoomsQuery request, CancellationToken cancellationToken)
    {
        var spec = new MeetingRoomsSpec(request.FloorId, request.MinCapacity, request.Active);
        var resources = await _resourceRepository.ListAsync(spec, cancellationToken);

        var items = new List<ResourceDto>();
        foreach (var resource in resources)
        {
            var resourceType = await _resourceTypeRepository.FirstOrDefaultAsync(new ResourceTypeByCodeSpec(resource.ResourceTypeCode), CancellationToken.None);
            var location = await _locationRepository.GetByIdAsync(resource.LocationId, CancellationToken.None);
            var floor = await _floorRepository.GetByIdAsync(resource.FloorId, CancellationToken.None);
            Zone? zone = null;
            if (resource.ZoneId.HasValue)
                zone = await _zoneRepository.GetByIdAsync(resource.ZoneId.Value, CancellationToken.None);

            items.Add(new ResourceDto(
                resource.Id,
                resource.Code,
                resource.Name,
                resource.ResourceTypeCode,
                resourceType!.Name,
                resource.LocationId,
                location!.Name,
                resource.FloorId,
                floor!.Code,
                resource.ZoneId,
                zone?.Name,
                resource.Capacity,
                resource.PublicQrId,
                resource.QrVersion,
                resource.Active,
                resource.Reservable,
                resource.CreatedAt,
                resource.UpdatedAt));
        }

        return Ardalis.Result.Result.Success<IReadOnlyList<ResourceDto>>(items);
    }
}