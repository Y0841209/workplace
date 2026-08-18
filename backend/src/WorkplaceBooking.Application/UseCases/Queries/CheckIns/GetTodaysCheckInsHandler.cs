using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Application.Interfaces;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.UseCases.Queries.CheckIns;

public class GetTodaysCheckInsHandler : IRequestHandler<GetTodaysCheckInsQuery, Result<IReadOnlyList<CheckInDto>>>
{
    private readonly IRepository<CheckIn> _checkInRepository;
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTodaysCheckInsHandler(
        IRepository<CheckIn> checkInRepository,
        IRepository<Reservation> reservationRepository,
        ICurrentUserService currentUser)
    {
        _checkInRepository = checkInRepository;
        _reservationRepository = reservationRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<CheckInDto>>> Handle(GetTodaysCheckInsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        // Get user's reservations for today
        var today = DateOnly.FromDateTime(DateTime.Today);
        var reservationSpec = new MyReservationsSpec(userId, null, today, today);
        var reservations = await _reservationRepository.ListAsync(reservationSpec, cancellationToken);

        var reservationIds = reservations.Select(r => r.Id).ToHashSet();
        if (reservationIds.Count == 0)
            return Result.Success<IReadOnlyList<CheckInDto>>(Array.Empty<CheckInDto>());

        // Get check-ins for today's reservations
        var spec = new CheckInsByReservationsSpec(reservationIds);
        var checkIns = await _checkInRepository.ListAsync(spec, cancellationToken);

        var items = checkIns.Select(c => new CheckInDto(
            c.Id,
            c.ReservationId,
            c.ResourceId,
            c.UserId,
            c.CheckedInAt,
            c.IpAddress,
            c.UserAgent)).ToList();

        return Result.Success<IReadOnlyList<CheckInDto>>(items);
    }
}