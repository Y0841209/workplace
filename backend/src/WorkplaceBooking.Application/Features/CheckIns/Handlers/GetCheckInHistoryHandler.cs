using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Application.Features.CheckIns.DTOs;
using WorkplaceBooking.Application.Features.CheckIns.Queries;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.CheckIns.Handlers;

public class GetCheckInHistoryHandler : IRequestHandler<GetCheckInHistoryQuery, Result<PagedResult<CheckInDto>>>
{
    private readonly IRepository<CheckIn> _checkInRepository;
    private readonly ICurrentUserService _currentUser;

    public GetCheckInHistoryHandler(
        IRepository<CheckIn> checkInRepository,
        ICurrentUserService currentUser)
    {
        _checkInRepository = checkInRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<CheckInDto>>> Handle(GetCheckInHistoryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        var spec = new CheckInsByUserSpec(userId, request.DateFrom, request.DateTo);
        var totalCount = await _checkInRepository.CountAsync(spec, cancellationToken);
        var checkIns = await _checkInRepository.ListAsync(spec.WithPaging(request.Page, request.PageSize), cancellationToken);

        var items = checkIns.Select(c => new CheckInDto(
            c.Id,
            c.ReservationId,
            c.ResourceId,
            c.UserId,
            c.CheckedInAt,
            c.IpAddress,
            c.UserAgent)).ToList();

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        return Result.Success(new PagedResult<CheckInDto>(items, totalCount, page, request.PageSize));
    }
}