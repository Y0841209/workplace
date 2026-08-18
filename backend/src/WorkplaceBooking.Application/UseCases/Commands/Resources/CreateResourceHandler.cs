using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Application.Interfaces;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Application.UseCases.Commands.Resources;

public class CreateResourceHandler : IRequestHandler<CreateResourceCommand, Result<ResourceDto>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<ResourceType> _resourceTypeRepository;
    private readonly IRepository<Location> _locationRepository;
    private readonly IRepository<Floor> _floorRepository;
    private readonly IRepository<Zone> _zoneRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateResourceHandler(
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

    public async Task<Result<ResourceDto>> Handle(CreateResourceCommand request, CancellationToken cancellationToken)
    {
        // Validate resource type exists
        var resourceType = await _resourceTypeRepository.GetByIdAsync(request.ResourceTypeCode, cancellationToken);
        if (resourceType == null || !resourceType.Active)
            return Result.NotFound("Resource type not found or inactive");

        // Validate location
        var location = await _locationRepository.GetByIdAsync(request.LocationId, cancellationToken);
        if (location == null || !location.Active)
            return Result.NotFound("Location not found or inactive");

        // Validate floor
        var floor = await _floorRepository.GetByIdAsync(request.FloorId, cancellationToken);
        if (floor == null || !floor.Active || floor.LocationId != request.LocationId)
            return Result.NotFound("Floor not found or doesn't belong to location");

        // Validate zone if provided
        Zone? zone = null;
        if (request.ZoneId.HasValue)
        {
            zone = await _zoneRepository.GetByIdAsync(request.ZoneId.Value, cancellationToken);
            if (zone == null || !zone.Active || zone.FloorId != request.FloorId)
                return Result.NotFound("Zone not found or doesn't belong to floor");
        }

        // Check code uniqueness
        var existingResource = await _resourceRepository.FirstOrDefaultAsync(
            new ResourceByCodeSpec(request.Code), cancellationToken);
        if (existingResource != null)
            return Result.Conflict($"Resource with code '{request.Code}' already exists");

        // Validate QR policy
        var requiresQr = request.ResourceTypeCode == "OPEN_WORKSPACE" || request.ResourceTypeCode == "CLOSED_OFFICE";
        var forbidsQr = request.ResourceTypeCode == "MEETING_ROOM";

        if (requiresQr && !request.PublicQrId.HasValue)
            return Result.Invalid(new[] { new Error("RESOURCE_QR_REQUIRED", "QR code is required for this resource type") });

        if (forbidsQr && request.PublicQrId.HasValue)
            return Result.Invalid(new[] { new Error("RESOURCE_QR_FORBIDDEN", "QR code is not allowed for meeting rooms") });

        // Create resource
        var publicQrId = request.PublicQrId ?? (requiresQr ? Guid.NewGuid() : (Guid?)null);
        var resourceResult = Resource.Create(
            request.Code,
            request.Name,
            request.ResourceTypeCode,
            request.LocationId,
            request.FloorId,
            request.ZoneId,
            request.Capacity,
            publicQrId);

        if (!resourceResult.IsSuccess)
            return Result.Error(resourceResult.Errors.First().Message);

        var resource = resourceResult.Value;
        await _resourceRepository.AddAsync(resource, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(resource, resourceType, location, floor, zone));
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