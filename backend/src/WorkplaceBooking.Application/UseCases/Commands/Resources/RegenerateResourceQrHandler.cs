using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;

namespace WorkplaceBooking.Application.UseCases.Commands.Resources;

public class RegenerateResourceQrHandler : IRequestHandler<RegenerateResourceQrCommand, Result<ResourceDto>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<ResourceType> _resourceTypeRepository;
    private readonly IRepository<Location> _locationRepository;
    private readonly IRepository<Floor> _floorRepository;
    private readonly IRepository<Zone> _zoneRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegenerateResourceQrHandler(
        IRepository<Resource> resourceRepository,
        IRepository<ResourceType> resourceTypeRepository,
        IRepository<Location> locationRepository,
        IRepository<Floor> floorRepository,
        IRepository<Zone> zoneRepository,
        IUnitOfWork unitOfWork)
    {
        _resourceRepository = resourceRepository;
        _resourceTypeRepository = resourceTypeRepository;
        _locationRepository = locationRepository;
        _floorRepository = floorRepository;
        _zoneRepository = zoneRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ResourceDto>> Handle(RegenerateResourceQrCommand request, CancellationToken cancellationToken)
    {
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
            return Result.NotFound("Resource not found");

        if (resource.ResourceTypeCode == "MEETING_ROOM")
            return Result.Error("Cannot regenerate QR for meeting rooms");

        resource.RegenerateQr();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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