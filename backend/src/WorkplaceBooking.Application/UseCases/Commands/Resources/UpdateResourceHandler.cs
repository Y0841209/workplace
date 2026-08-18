using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Application.Interfaces;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;

namespace WorkplaceBooking.Application.UseCases.Commands.Resources;

public class UpdateResourceHandler : IRequestHandler<UpdateResourceCommand, Result<ResourceDto>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<ResourceType> _resourceTypeRepository;
    private readonly IRepository<Location> _locationRepository;
    private readonly IRepository<Floor> _floorRepository;
    private readonly IRepository<Zone> _zoneRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateResourceHandler(
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

    public async Task<Result<ResourceDto>> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
            return Result.NotFound("Resource not found");

        // Validate resource type if changing
        ResourceType? resourceType = null;
        if (request.ResourceTypeCode != null && request.ResourceTypeCode != resource.ResourceTypeCode)
        {
            var rt = await _resourceTypeRepository.GetByIdAsync(request.ResourceTypeCode, cancellationToken);
            if (rt == null || !rt.Active)
                return Result.NotFound("Resource type not found or inactive");
            resourceType = rt;
        }

        // Validate location
        Location? location = null;
        if (request.LocationId.HasValue && request.LocationId != resource.LocationId)
        {
            var loc = await _locationRepository.GetByIdAsync(request.LocationId.Value, cancellationToken);
            if (loc == null || !loc.Active)
                return Result.NotFound("Location not found or inactive");
            location = loc;
        }

        // Validate floor
        Floor? floor = null;
        if (request.FloorId.HasValue && request.FloorId != resource.FloorId)
        {
            var fl = await _floorRepository.GetByIdAsync(request.FloorId.Value, cancellationToken);
            if (fl == null || !fl.Active || fl.LocationId != (request.LocationId ?? resource.LocationId))
                return Result.NotFound("Floor not found or doesn't belong to location");
            floor = fl;
        }

        // Validate zone if provided
        Zone? zone = null;
        if (request.ZoneId.HasValue && request.ZoneId != resource.ZoneId)
        {
            var zn = await _zoneRepository.GetByIdAsync(request.ZoneId.Value, cancellationToken);
            if (zn == null || !zn.Active || zn.FloorId != (request.FloorId ?? resource.FloorId))
                return Result.NotFound("Zone not found or doesn't belong to floor");
            zone = zn;
        }

        // Validate QR policy if changing type or QR
        var newTypeCode = request.ResourceTypeCode ?? resource.ResourceTypeCode;
        var newPublicQrId = request.PublicQrId ?? resource.PublicQrId;

        var requiresQr = newTypeCode == "OPEN_WORKSPACE" || newTypeCode == "CLOSED_OFFICE";
        var forbidsQr = newTypeCode == "MEETING_ROOM";

        if (requiresQr && newPublicQrId == null)
            return Result.Invalid(new[] { new Error("RESOURCE_QR_REQUIRED", "QR code is required for this resource type") });

        if (forbidsQr && newPublicQrId.HasValue)
            return Result.Invalid(new[] { new Error("RESOURCE_QR_FORBIDDEN", "QR code is not allowed for meeting rooms") });

        // Apply updates
        var updateResult = resource.Update(
            request.Name,
            request.ResourceTypeCode,
            request.LocationId,
            request.FloorId,
            request.ZoneId,
            request.Capacity,
            request.PublicQrId,
            request.Active,
            request.Reservable);

        if (!updateResult.IsSuccess)
            return Result.Error(updateResult.Errors.First().Message);

        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Return updated DTO
        var finalType = resourceType ?? await _resourceTypeRepository.GetByIdAsync(resource.ResourceTypeCode, CancellationToken.None);
        var finalLocation = location ?? await _locationRepository.GetByIdAsync(resource.LocationId, CancellationToken.None);
        var finalFloor = floor ?? await _floorRepository.GetByIdAsync(resource.FloorId, CancellationToken.None);
        var finalZone = zone ?? (resource.ZoneId.HasValue ? await _zoneRepository.GetByIdAsync(resource.ZoneId.Value, CancellationToken.None) : null);

        return Result.Success(ToDto(resource, finalType!, finalLocation!, finalFloor!, finalZone));
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