using Ardalis.Result;
using AutoMapper;
using MediatR;
using WorkplaceBooking.Application.Features.Resources.Commands;
using WorkplaceBooking.Application.Features.Resources.DTOs;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Resources.Handlers;

public class RegenerateResourceQrHandler : IRequestHandler<RegenerateResourceQrCommand, Ardalis.Result.Result<ResourceDto>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<ResourceType> _resourceTypeRepository;
    private readonly IRepository<Location> _locationRepository;
    private readonly IRepository<Floor> _floorRepository;
    private readonly IRepository<Zone> _zoneRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RegenerateResourceQrHandler(
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

    public async Task<Ardalis.Result.Result<ResourceDto>> Handle(RegenerateResourceQrCommand request, CancellationToken cancellationToken)
    {
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
            return Ardalis.Result.Result.NotFound("Resource not found");

        if (resource.ResourceTypeCode == "MEETING_ROOM")
            return Ardalis.Result.Result.Error("Cannot regenerate QR for meeting rooms");

        resource.RegenerateQr();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var resourceType = await _resourceTypeRepository.FirstOrDefaultAsync(new ResourceTypeByCodeSpec(resource.ResourceTypeCode), CancellationToken.None);
        var location = await _locationRepository.GetByIdAsync(resource.LocationId, CancellationToken.None);
        var floor = await _floorRepository.GetByIdAsync(resource.FloorId, CancellationToken.None);
        Zone? zone = null;
        if (resource.ZoneId.HasValue)
            zone = await _zoneRepository.GetByIdAsync(resource.ZoneId.Value, CancellationToken.None);

        return Ardalis.Result.Result.Success(new ResourceDto(
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
}