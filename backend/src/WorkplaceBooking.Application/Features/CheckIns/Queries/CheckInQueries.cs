using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.DTOs;
using WorkplaceBooking.Application.Features.CheckIns.DTOs;

namespace WorkplaceBooking.Application.Features.CheckIns.Queries;

public record GetCheckInHistoryQuery(
    int Page = 1,
    int PageSize = 20,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null) : IRequest<Ardalis.Result.Result<WorkplaceBooking.Application.Common.DTOs.PagedResult<CheckInDto>>>;

public record GetResourceCheckInsQuery(
    Guid ResourceId,
    DateOnly? Date = null) : IRequest<Ardalis.Result.Result<IReadOnlyList<CheckInDto>>>;

public record GetTodaysCheckInsQuery : IRequest<Ardalis.Result.Result<IReadOnlyList<CheckInDto>>>;