using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Application.Features.Reservations.DTOs;
using WorkplaceBooking.Application.Features.Reservations.Queries;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Reservations.Handlers;

public class GetMyReservationsHandler : IRequestHandler<GetMyReservationsQuery, Result<PagedResult<ReservationDto>>>
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<AppUser> _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyReservationsHandler(
        IRepository<Reservation> reservationRepository,
        IRepository<Resource> resourceRepository,
        IRepository<AppUser> userRepository,
        ICurrentUserService currentUser)
    {
        _reservationRepository = reservationRepository;
        _resourceRepository = resourceRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<ReservationDto>>> Handle(GetMyReservationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        var spec = new MyReservationsSpec(userId, request.Status, request.DateFrom, request.DateTo);
        var totalCount = await _reservationRepository.CountAsync(spec, cancellationToken);
        var reservations = await _reservationRepository.ListAsync(spec.WithPaging(request.Page, request.PageSize), cancellationToken);

        var items = new List<ReservationDto>();
        foreach (var reservation in reservations)
        {
            var resource = await _resourceRepository.GetByIdAsync(reservation.ResourceId, CancellationToken.None);
            var user = await _userRepository.GetByIdAsync(reservation.UserId, CancellationToken.None);

            items.Add(new ReservationDto(
                reservation.Id,
                reservation.ResourceId,
                resource?.Code ?? string.Empty,
                resource?.Name ?? string.Empty,
                resource?.ResourceTypeCode ?? string.Empty,
                reservation.UserId,
                user?.DisplayName ?? string.Empty,
                user?.Email ?? string.Empty,
                reservation.ReservationDate,
                reservation.StartTime,
                reservation.EndTime,
                reservation.Status.ToString(),
                reservation.Title,
                reservation.Description,
                reservation.AttendeeCount,
                reservation.SupportChangeReason,
                reservation.CheckedInAt,
                reservation.CheckedOutAt,
                reservation.CancelledAt,
                reservation.CancellationReason,
                reservation.CreatedAt,
                reservation.UpdatedAt));
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Result.Success(new PagedResult<ReservationDto>(pagedItems, totalCount, page, request.PageSize));
    }
}