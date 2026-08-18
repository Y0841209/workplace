using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Features.Resources.DTOs;

namespace WorkplaceBooking.Application.Features.Resources.Queries;

public record GetAvailabilityQuery(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? ResourceTypeCode = null,
    Guid? FloorId = null,
    Guid? ZoneId = null,
    int? MinCapacity = null) : IRequest<Result<IReadOnlyList<AvailabilitySlotDto>>>;

public record GetResourcesByFloorQuery(
    Guid FloorId,
    string? ResourceTypeCode = null,
    bool? Active = null,
    bool? Reservable = null) : IRequest<Result<ResourcesByFloorDto>>;

public record GetMeetingRoomsQuery(
    Guid? FloorId = null,
    int? MinCapacity = null,
    bool? Active = null) : IRequest<Result<IReadOnlyList<ResourceDto>>>;