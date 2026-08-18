using System;
using System.Threading.Tasks;

namespace WorkplaceBooking.Application.Common.Interfaces;

public interface IUserAuthorizationService
{
    Task<bool> CanModifyReservationAsync(Guid userId, Guid reservationId, bool isSupportUser, CancellationToken cancellationToken = default);
    Task<bool> CanCancelReservationAsync(Guid userId, Guid reservationId, bool isSupportUser, CancellationToken cancellationToken = default);
    Task<bool> CanCheckInAsync(Guid userId, Guid reservationId, Guid scannedQrId, CancellationToken cancellationToken = default);
}