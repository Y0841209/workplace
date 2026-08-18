using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;
using Ardalis.Result;

namespace WorkplaceBooking.Infrastructure.Services;

public class UserAuthorizationService : IUserAuthorizationService
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IRepository<UserApplicationRole> _userRoleRepository;
    private readonly IRepository<UserBusinessProfile> _userProfileRepository;

    public UserAuthorizationService(
        IRepository<Reservation> reservationRepository,
        IRepository<UserApplicationRole> userRoleRepository,
        IRepository<UserBusinessProfile> userProfileRepository)
    {
        _reservationRepository = reservationRepository;
        _userRoleRepository = userRoleRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<bool> CanModifyReservationAsync(Guid userId, Guid reservationId, bool isSupportUser, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
        if (reservation == null) return false;

        if (isSupportUser) return true;

        return reservation.UserId == userId;
    }

    public async Task<bool> CanCancelReservationAsync(Guid userId, Guid reservationId, bool isSupportUser, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
        if (reservation == null) return false;

        if (isSupportUser) return true;

        return reservation.UserId == userId;
    }

    public async Task<bool> CanCheckInAsync(Guid userId, Guid reservationId, Guid scannedQrId, CancellationToken cancellationToken = default)
    {
        // This is validated in the check-in handler with more detailed checks
        return true;
    }
}