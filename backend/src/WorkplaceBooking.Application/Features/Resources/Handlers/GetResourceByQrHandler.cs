using Ardalis.Result;
using Ardalis.Specification;
using MediatR;
using WorkplaceBooking.Application.Features.Resources.DTOs;
using WorkplaceBooking.Application.Features.Resources.Queries;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;

namespace WorkplaceBooking.Application.Features.Resources.Handlers;

public class GetResourceByQrHandler : IRequestHandler<GetResourceByQrQuery, Result<ResourceDto>>
{
    private readonly IRepository<Resource> _resourceRepository;

    public GetResourceByQrHandler(IRepository<Resource> resourceRepository)
    {
        _resourceRepository = resourceRepository;
    }

    public async Task<Result<ResourceDto>> Handle(GetResourceByQrQuery request, CancellationToken cancellationToken)
    {
        var spec = new ResourceByQrSpec(request.PublicQrId);
        var resource = await _resourceRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (resource == null)
        {
            return Result.NotFound("Resource not found");
        }

        var dto = new ResourceDto(
            resource.Id,
            resource.Code,
            resource.Name,
            resource.ResourceTypeCode,
            resource.ResourceType?.Name ?? string.Empty,
            resource.LocationId,
            resource.Location?.Name ?? string.Empty,
            resource.FloorId,
            resource.Floor?.Code ?? string.Empty,
            resource.ZoneId,
            resource.Zone?.Name,
            resource.Capacity,
            resource.PublicQrId,
            resource.QrVersion,
            resource.Active,
            resource.Reservable,
            resource.CreatedAt,
            resource.UpdatedAt);

        return Result.Success(dto);
    }
}

public class ResourceByQrSpec : Specification<Resource>
{
    public ResourceByQrSpec(Guid publicQrId)
    {
        Query.Where(r => r.PublicQrId == publicQrId);
        Query.Include(r => r.ResourceType);
        Query.Include(r => r.Location);
        Query.Include(r => r.Floor);
        Query.Include(r => r.Zone);
    }
}