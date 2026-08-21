using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Features.Resources.DTOs;
using WorkplaceBooking.Application.Features.Resources.Queries;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Resources.Handlers;

public class GetResourcesByFloorHandler : IRequestHandler<GetResourcesByFloorQuery, Ardalis.Result.Result<ResourcesByFloorDto>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<Floor> _floorRepository;
    private readonly IRepository<ResourceType> _resourceTypeRepository;
    private readonly IRepository<Location> _locationRepository;
    private readonly IRepository<Zone> _zoneRepository;

    public GetResourcesByFloorHandler(
        IRepository<Resource> resourceRepository,
        IRepository<Floor> floorRepository,
        IRepository<ResourceType> resourceTypeRepository,
        IRepository<Location> locationRepository,
        IRepository<Zone> zoneRepository)
    {
        _resourceRepository = resourceRepository;
        _floorRepository = floorRepository;
        _resourceTypeRepository = resourceTypeRepository;
        _locationRepository = locationRepository;
        _zoneRepository = zoneRepository;
    }

    public async Task<Ardalis.Result.Result<ResourcesByFloorDto>> Handle(GetResourcesByFloorQuery request, CancellationToken cancellationToken)
    {
        var floorEntity = await _floorRepository.GetByIdAsync(request.FloorId, cancellationToken);
        if (floorEntity == null)
            return Ardalis.Result.Result.NotFound("Floor not found");

        var spec = new ResourcesByFloorSpec(request.FloorId, request.ResourceTypeCode, request.Active, request.Reservable);
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

        return Ardalis.Result.Result.Success(new ResourcesByFloorDto(
            floorEntity.Id,
            floorEntity.Name,
            floorEntity.FloorNumber,
            items));
    }
}