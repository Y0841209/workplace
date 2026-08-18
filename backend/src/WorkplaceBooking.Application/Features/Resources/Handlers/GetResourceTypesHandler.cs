using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Application.Features.Resources.DTOs;
using WorkplaceBooking.Application.Features.Resources.Queries;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Resources.Handlers;

public class GetResourceTypesHandler : IRequestHandler<GetResourceTypesQuery, Result<IReadOnlyList<ResourceTypeDto>>>
{
    private readonly IRepository<ResourceType> _resourceTypeRepository;

    public GetResourceTypesHandler(IRepository<ResourceType> resourceTypeRepository)
    {
        _resourceTypeRepository = resourceTypeRepository;
    }

    public async Task<Result<IReadOnlyList<ResourceTypeDto>>> Handle(GetResourceTypesQuery request, CancellationToken cancellationToken)
    {
        var resourceTypes = await _resourceTypeRepository.ListAsync(new ActiveResourceTypesSpec(), cancellationToken);
        var items = resourceTypes.Select(rt => new ResourceTypeDto(
            rt.Code,
            rt.Name,
            rt.QrRequired,
            rt.CheckinRequired,
            rt.Active)).ToList();

        return Result.Success<IReadOnlyList<ResourceTypeDto>>(items);
    }
}