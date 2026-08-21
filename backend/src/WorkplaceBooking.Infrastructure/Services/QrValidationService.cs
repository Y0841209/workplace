using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;
using Ardalis.Result;

namespace WorkplaceBooking.Infrastructure.Services;

public class QrValidationService : IQrValidationService
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<Reservation> _reservationRepository;

    public QrValidationService(
        IRepository<Resource> resourceRepository,
        IRepository<Reservation> reservationRepository)
    {
        _resourceRepository = resourceRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task<Result<Resource>> ValidateQrAsync(Guid publicQrId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Find resource by QR
        var resource = await _resourceRepository.FirstOrDefaultAsync(
            new ResourceByPublicQrSpec(publicQrId), cancellationToken);

        if (resource == null)
            return Result.NotFound("Invalid QR code");

        // Check resource type allows check-in
        if (resource.ResourceTypeCode != "OPEN_WORKSPACE" && resource.ResourceTypeCode != "CLOSED_OFFICE")
            return Result.Error("Check-in only allowed for offices");

        // Check for active reservation for this user today
        var today = DateOnly.FromDateTime(DateTime.Today);
        var reservation = await _reservationRepository.FirstOrDefaultAsync(
            new ActiveReservationForUserSpec(userId, today), cancellationToken);

        if (reservation == null)
            return Result.Error("No active reservation found for today");

        if (reservation.ResourceId != resource.Id)
            return Result.Error("QR code does not match your reservation");

        // Check time window (15 min before start to 15 min after end)
        var now = DateTimeOffset.Now;
        var start = reservation.ReservationDate.ToDateTime(reservation.StartTime);
        var end = reservation.ReservationDate.ToDateTime(reservation.EndTime);
        var windowStart = start.AddMinutes(-15);
        var windowEnd = end.AddMinutes(15);

        if (now < windowStart || now > windowEnd)
            return Result.Error("Check-in only allowed within 15 minutes of reservation time");

        return Result.Success(resource);
    }
}