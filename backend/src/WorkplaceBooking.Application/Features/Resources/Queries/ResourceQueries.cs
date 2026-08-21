using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.DTOs;
using WorkplaceBooking.Application.Features.Resources.DTOs;

namespace WorkplaceBooking.Application.Features.Resources.Queries;

public record GetResourceByIdQuery(
    Guid ResourceId) : IRequest<Ardalis.Result.Result<ResourceDto>>;

public record GetResourcesQuery(
    int Page = 1,
    int PageSize = 20,
    string? ResourceTypeCode = null,
    Guid? FloorId = null,
    Guid? ZoneId = null,
    bool? Active = null,
    bool? Reservable = null,
    string? Search = null) : IRequest<Ardalis.Result.Result<WorkplaceBooking.Application.Common.DTOs.PagedResult<ResourceDto>>>;

public record GetResourceTypesQuery : IRequest<Ardalis.Result.Result<IReadOnlyList<ResourceTypeDto>>>;

public record GetAvailabilityQuery(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? ResourceTypeCode = null,
    Guid? FloorId = null,
    Guid? ZoneId = null,
    int? MinCapacity = null) : IRequest<Ardalis.Result.Result<IReadOnlyList<AvailabilitySlotDto>>>;

public record GetResourcesByFloorQuery(
    Guid FloorId,
    string? ResourceTypeCode = null,
    bool? Active = null,
    bool? Reservable = null) : IRequest<Ardalis.Result.Result<ResourcesByFloorDto>>;

public record GetMeetingRoomsQuery(
    Guid? FloorId = null,
    int? MinCapacity = null,
    bool? Active = null) : IRequest<Ardalis.Result.Result<IReadOnlyList<ResourceDto>>>;

public record GetResourceByQrQuery(
    Guid PublicQrId) : IRequest<Ardalis.Result.Result<ResourceDto>>;