using WorkplaceBooking.Domain.Entities;
using Ardalis.Result;

namespace WorkplaceBooking.Domain.Interfaces;

public interface IReservationPolicyService
{
    Task<int> GetMaxFutureReservationsAsync(CancellationToken cancellationToken = default);
    Task<bool> HasActiveExceptionAsync(Guid userId, string? resourceTypeCode, CancellationToken cancellationToken = default);
    Task<bool> CanReserveAsync(Guid userId, string resourceTypeCode, CancellationToken cancellationToken = default);
}

public interface IAvailabilityService
{
    Task<bool> IsAvailableAsync(
        Guid resourceId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken cancellationToken = default,
        Guid? excludeReservationId = null);
}

public interface IQrValidationService
{
    Task<Result<Resource>> ValidateQrAsync(Guid publicQrId, Guid userId, CancellationToken cancellationToken = default);
}

public interface IUserAuthorizationService
{
    Task<bool> CanModifyReservationAsync(Guid userId, Guid reservationId, bool isSupportUser, CancellationToken cancellationToken = default);
    Task<bool> CanCancelReservationAsync(Guid userId, Guid reservationId, bool isSupportUser, CancellationToken cancellationToken = default);
    Task<bool> CanCheckInAsync(Guid userId, Guid reservationId, Guid scannedQrId, CancellationToken cancellationToken = default);
}