using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Application.Interfaces;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.UseCases.Queries.Reservations;

public class GetMyReservationsHandler : IRequestHandler<GetMyReservationsQuery, Result<PagedResult<ReservationDto>>>
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyReservationsHandler(
        IRepository<Reservation> reservationRepository,
        ICurrentUserService currentUser)
    {
        _reservationRepository = reservationRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<ReservationDto>>> Handle(GetMyReservationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        var spec = new MyReservationsSpec(userId, request.Status, request.DateFrom, request.DateTo);
        var totalCount = await _reservationRepository.CountAsync(spec, CancellationToken.None);
        var reservations = await _reservationRepository.ListAsync(spec, CancellationToken.None);

        // This would need proper resource/user data loaded - simplified for now
        var items = reservations.Select(r => new ReservationDto(
            r.Id, r.ResourceId, string.Empty, string.Empty, string.Empty,
            r.UserId, string.Empty, string.Empty,
            r.ReservationDate, r.StartTime, r.EndTime,
            r.Status.ToString(), r.Title, r.Description, r.AttendeeCount,
            r.SupportChangeReason, r.CheckedInAt, r.CheckedOutAt,
            r.CancelledAt, r.CancellationReason, r.CreatedAt, r.UpdatedAt))
            .ToList();

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Result.Success(new PagedResult<ReservationDto>(pagedItems, totalCount, page, request.PageSize));
    }
}