using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;

namespace WorkplaceBooking.Application.UseCases.Queries.CheckIns;

public record GetCheckInHistoryQuery(
    int Page = 1,
    int PageSize = 20,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null) : IRequest<Result<PagedResult<CheckInDto>>>;

public record GetResourceCheckInsQuery(
    Guid ResourceId,
    DateOnly? Date = null) : IRequest<Result<IReadOnlyList<CheckInDto>>>;

public record GetTodaysCheckInsQuery : IRequest<Result<IReadOnlyList<CheckInDto>>>;