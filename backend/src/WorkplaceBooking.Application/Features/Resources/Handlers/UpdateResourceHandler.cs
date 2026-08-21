using Ardalis.Result;
using AutoMapper;
using MediatR;
using WorkplaceBooking.Application.Features.Resources.Commands;
using WorkplaceBooking.Application.Features.Resources.DTOs;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Resources.Handlers;

public class UpdateResourceHandler : IRequestHandler<UpdateResourceCommand, Ardalis.Result.Result<ResourceDto>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<ResourceType> _resourceTypeRepository;
    private readonly IRepository<Location> _locationRepository;
    private readonly IRepository<Floor> _floorRepository;
    private readonly IRepository<Zone> _zoneRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateResourceHandler(
        IRepository<Resource> resourceRepository,
        IRepository<ResourceType> resourceTypeRepository,
        IRepository<Location> locationRepository,
        IRepository<Floor> floorRepository,
        IRepository<Zone> zoneRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _resourceRepository = resourceRepository;
        _resourceTypeRepository = resourceTypeRepository;
        _locationRepository = locationRepository;
        _floorRepository = floorRepository;
        _zoneRepository = zoneRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Ardalis.Result.Result<ResourceDto>> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
            return Ardalis.Result.Result.NotFound("Resource not found");

        // Validate resource type if changing
        ResourceType? resourceType = null;
        if (request.ResourceTypeCode != null && request.ResourceTypeCode != resource.ResourceTypeCode)
        {
            var rt = await _resourceTypeRepository.FirstOrDefaultAsync(new ResourceTypeByCodeSpec(request.ResourceTypeCode), cancellationToken);
            if (rt == null || !rt.Active)
                return Ardalis.Result.Result.NotFound("Resource type not found or inactive");
            resourceType = rt;
        }

        // Validate location
        Location? location = null;
        if (request.LocationId.HasValue && request.LocationId != resource.LocationId)
        {
            var loc = await _locationRepository.GetByIdAsync(request.LocationId.Value, cancellationToken);
            if (loc == null || !loc.Active)
                return Ardalis.Result.Result.NotFound("Location not found or inactive");
            location = loc;
        }

        // Validate floor
        Floor? floor = null;
        if (request.FloorId.HasValue && request.FloorId != resource.FloorId)
        {
            var fl = await _floorRepository.GetByIdAsync(request.FloorId.Value, cancellationToken);
            if (fl == null || !fl.Active || fl.LocationId != (request.LocationId ?? resource.LocationId))
                return Ardalis.Result.Result.NotFound("Floor not found or doesn't belong to location");
            floor = fl;
        }

        // Validate zone if provided
        Zone? zone = null;
        if (request.ZoneId.HasValue && request.ZoneId != resource.ZoneId)
        {
            var zn = await _zoneRepository.GetByIdAsync(request.ZoneId.Value, cancellationToken);
            if (zn == null || !zn.Active || zn.FloorId != (request.FloorId ?? resource.FloorId))
                return Ardalis.Result.Result.NotFound("Zone not found or doesn't belong to floor");
            zone = zn;
        }

        // Validate QR policy if changing type or QR
        var newTypeCode = request.ResourceTypeCode ?? resource.ResourceTypeCode;
        var newPublicQrId = request.PublicQrId ?? resource.PublicQrId;

        var requiresQr = newTypeCode == "OPEN_WORKSPACE" || newTypeCode == "CLOSED_OFFICE";
        var forbidsQr = newTypeCode == "MEETING_ROOM";

        if (requiresQr && newPublicQrId == null)
            return Ardalis.Result.Result.Invalid(new[] { new ValidationError("RESOURCE_QR_REQUIRED", "QR code is required for this resource type", "RESOURCE_QR_REQUIRED", ValidationSeverity.Error) });

        if (forbidsQr && newPublicQrId.HasValue)
            return Ardalis.Result.Result.Invalid(new[] { new ValidationError("RESOURCE_QR_FORBIDDEN", "QR code is not allowed for meeting rooms", "RESOURCE_QR_FORBIDDEN", ValidationSeverity.Error) });

        // Apply updates
        resource.Update(
            request.Name,
            request.ResourceTypeCode,
            request.LocationId,
            request.FloorId,
            request.ZoneId,
            request.Capacity,
            request.PublicQrId,
            request.Active,
            request.Reservable);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return updated DTO
        var finalType = resourceType ?? await _resourceTypeRepository.FirstOrDefaultAsync(new ResourceTypeByCodeSpec(resource.ResourceTypeCode), CancellationToken.None);
        var finalLocation = location ?? await _locationRepository.GetByIdAsync(resource.LocationId, CancellationToken.None);
        var finalFloor = floor ?? await _floorRepository.GetByIdAsync(resource.FloorId, CancellationToken.None);
        var finalZone = zone ?? (resource.ZoneId.HasValue ? await _zoneRepository.GetByIdAsync(resource.ZoneId.Value, CancellationToken.None) : null);

        return Ardalis.Result.Result.Success(new ResourceDto(
            resource.Id,
            resource.Code,
            resource.Name,
            resource.ResourceTypeCode,
            finalType!.Name,
            resource.LocationId,
            finalLocation!.Name,
            resource.FloorId,
            finalFloor!.Code,
            resource.ZoneId,
            finalZone?.Name,
            resource.Capacity,
            resource.PublicQrId,
            resource.QrVersion,
            resource.Active,
            resource.Reservable,
            resource.CreatedAt,
            resource.UpdatedAt));
    }
}