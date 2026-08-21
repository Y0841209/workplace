using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Features.Resources.Commands;
using WorkplaceBooking.Application.Features.Resources.DTOs;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Application.Features.Resources.Handlers;

public class ImportResourcesHandler : IRequestHandler<ImportResourcesCommand, Ardalis.Result.Result<int>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<ResourceType> _resourceTypeRepository;
    private readonly IRepository<Location> _locationRepository;
    private readonly IRepository<Floor> _floorRepository;
    private readonly IRepository<Zone> _zoneRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ImportResourcesHandler(
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

    public async Task<Ardalis.Result.Result<int>> Handle(ImportResourcesCommand request, CancellationToken cancellationToken)
    {
        var imported = 0;
        var errors = new List<string>();

        foreach (var cmd in request.Resources)
        {
            try
            {
                // Validate resource type
                var resourceType = await _resourceTypeRepository.FirstOrDefaultAsync(new ResourceTypeByCodeSpec(cmd.ResourceTypeCode), cancellationToken);
                if (resourceType == null || !resourceType.Active)
                {
                    errors.Add($"{cmd.Code}: Resource type '{cmd.ResourceTypeCode}' not found");
                    continue;
                }

                // Validate location
                var location = await _locationRepository.GetByIdAsync(cmd.LocationId, cancellationToken);
                if (location == null || !location.Active)
                {
                    errors.Add($"{cmd.Code}: Location not found");
                    continue;
                }

                // Validate floor
                var floor = await _floorRepository.GetByIdAsync(cmd.FloorId, cancellationToken);
                if (floor == null || !floor.Active || floor.LocationId != cmd.LocationId)
                {
                    errors.Add($"{cmd.Code}: Floor not found or doesn't belong to location");
                    continue;
                }

                // Validate zone if provided
                if (cmd.ZoneId.HasValue)
                {
                    var zone = await _zoneRepository.GetByIdAsync(cmd.ZoneId.Value, cancellationToken);
                    if (zone == null || !zone.Active || zone.FloorId != cmd.FloorId)
                    {
                        errors.Add($"{cmd.Code}: Zone not found or doesn't belong to floor");
                        continue;
                    }
                }

                // Check code uniqueness
                var existing = await _resourceRepository.FirstOrDefaultAsync(
                    new ResourceByCodeSpec(cmd.Code), cancellationToken);
                if (existing != null)
                {
                    errors.Add($"{cmd.Code}: Resource code already exists");
                    continue;
                }

                // Validate QR policy
                var requiresQr = cmd.ResourceTypeCode == "OPEN_WORKSPACE" || cmd.ResourceTypeCode == "CLOSED_OFFICE";
                var forbidsQr = cmd.ResourceTypeCode == "MEETING_ROOM";

                if (requiresQr && !cmd.PublicQrId.HasValue)
                {
                    errors.Add($"{cmd.Code}: QR code required for this resource type");
                    continue;
                }

                if (forbidsQr && cmd.PublicQrId.HasValue)
                {
                    errors.Add($"{cmd.Code}: QR code not allowed for meeting rooms");
                    continue;
                }

                var publicQrId = cmd.PublicQrId ?? (requiresQr ? Guid.NewGuid() : (Guid?)null);
                var resourceResult = Resource.Create(
                    cmd.Code,
                    cmd.Name,
                    cmd.ResourceTypeCode,
                    cmd.LocationId,
                    cmd.FloorId,
                    cmd.ZoneId,
                    cmd.Capacity,
                    publicQrId);

                if (!resourceResult.IsSuccess)
                {
                    errors.Add($"{cmd.Code}: {resourceResult.Error.Message}");
                    continue;
                }

                await _resourceRepository.AddAsync(resourceResult.Value, cancellationToken);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"{cmd.Code}: {ex.Message}");
            }
        }

        if (imported > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (errors.Count > 0)
        {
            // Log errors or handle as needed - for now just return the count
            return Ardalis.Result.Result.Success(imported);
        }

        return Ardalis.Result.Result.Success(imported);
    }
}