using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;

namespace WorkplaceBooking.Application.UseCases.Queries.Resources;

public record GetResourceByIdQuery(
    Guid ResourceId) : IRequest<Result<ResourceDto>>;

public record GetResourcesQuery(
    int Page = 1,
    int PageSize = 20,
    string? ResourceTypeCode = null,
    Guid? FloorId = null,
    Guid? ZoneId = null,
    bool? Active = null,
    bool? Reservable = null,
    string? Search = null) : IRequest<Result<PagedResult<ResourceDto>>>;

public record GetResourceTypesQuery : IRequest<Result<IReadOnlyList<ResourceTypeDto>>>;