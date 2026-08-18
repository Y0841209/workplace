using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.UseCases.Queries.Resources;

public class GetResourcesHandler : IRequestHandler<GetResourcesQuery, Result<PagedResult<ResourceDto>>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<ResourceType> _resourceTypeRepository;
    private readonly IRepository<Location> _locationRepository;
    private readonly IRepository<Floor> _floorRepository;
    private readonly IRepository<Zone> _zoneRepository;

    public GetResourcesHandler(
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

    public async Task<Result<PagedResult<ResourceDto>>> Handle(GetResourcesQuery request, CancellationToken cancellationToken)
    {
        var spec = new ResourcesFilteredSpec(
            request.ResourceTypeCode,
            request.FloorId,
            request.ZoneId,
            request.Active,
            request.Reservable,
            request.Search);

        var totalCount = await _resourceRepository.CountAsync(spec, cancellationToken);
        var resources = await _resourceRepository.ListAsync(spec.WithPaging(request.Page, request.PageSize), cancellationToken);

        var items = new List<ResourceDto>();
        foreach (var resource in resources)
        {
            var resourceType = await _resourceTypeRepository.GetByIdAsync(resource.ResourceTypeCode, CancellationToken.None);
            var location = await _locationRepository.GetByIdAsync(resource.LocationId, CancellationToken.None);
            var floor = await _floorRepository.GetByIdAsync(resource.FloorId, CancellationToken.None);
            Zone? zone = null;
            if (resource.ZoneId.HasValue)
                zone = await _zoneRepository.GetByIdAsync(resource.ZoneId.Value, CancellationToken.None);

            items.Add(ToDto(resource, resourceType!, location!, floor!, zone));
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Result.Success(new PagedResult<ResourceDto>(pagedItems, totalCount, page, request.PageSize));
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