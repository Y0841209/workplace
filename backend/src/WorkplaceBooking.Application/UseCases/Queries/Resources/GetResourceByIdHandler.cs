using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.UseCases.Queries.Resources;

public class GetResourceByIdHandler : IRequestHandler<GetResourceByIdQuery, Result<ResourceDto>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<ResourceType> _resourceTypeRepository;
    private readonly IRepository<Location> _locationRepository;
    private readonly IRepository<Floor> _floorRepository;
    private readonly IRepository<Zone> _zoneRepository;

    public GetResourceByIdHandler(
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

    public async Task<Result<ResourceDto>> Handle(GetResourceByIdQuery request, CancellationToken cancellationToken)
    {
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
            return Result.NotFound("Resource not found");

        var resourceType = await _resourceTypeRepository.GetByIdAsync(resource.ResourceTypeCode, CancellationToken.None);
        var location = await _locationRepository.GetByIdAsync(resource.LocationId, CancellationToken.None);
        var floor = await _floorRepository.GetByIdAsync(resource.FloorId, CancellationToken.None);
        Zone? zone = null;
        if (resource.ZoneId.HasValue)
            zone = await _zoneRepository.GetByIdAsync(resource.ZoneId.Value, CancellationToken.None);

        return Result.Success(ToDto(resource, resourceType!, location!, floor!, zone));
    }

    private static ResourceDto ToDto(Resource resource, ResourceType resourceType, Location location, Floor floor, Zone? zone)
    {
        return new ResourceDto(
            resource.Id,
            resource.Code,
            resource.Name,
            resource.ResourceTypeCode,
            resourceType.Name,
            resource.LocationId,
            location.Name,
            resource.FloorId,
            floor.Code,
            resource.ZoneId,
            zone?.Name,
            resource.Capacity,
            resource.PublicQrId,
            resource.QrVersion,
            resource.Active,
            resource.Reservable,
            resource.CreatedAt,
            resource.UpdatedAt);
    }
}