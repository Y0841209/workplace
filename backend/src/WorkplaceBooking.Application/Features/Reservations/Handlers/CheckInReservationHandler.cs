using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Application.Features.CheckIns.DTOs;
using WorkplaceBooking.Application.Features.Reservations.Commands;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Reservations.Handlers;

public class CheckInReservationHandler : IRequestHandler<CheckInReservationCommand, Ardalis.Result.Result<CheckInDto>>
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<CheckIn> _checkInRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CheckInReservationHandler(
        IRepository<Reservation> reservationRepository,
        IRepository<Resource> resourceRepository,
        IRepository<CheckIn> checkInRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _reservationRepository = reservationRepository;
        _resourceRepository = resourceRepository;
        _checkInRepository = checkInRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Ardalis.Result.Result<CheckInDto>> Handle(CheckInReservationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation == null)
            return Ardalis.Result.Result.NotFound("Reservation not found");

        // Check ownership
        if (reservation.UserId != userId)
            return Ardalis.Result.Result.Forbidden("Only reservation owner can check in");

        // Validate status
        if (reservation.Status != ReservationStatus.CONFIRMED)
            return Ardalis.Result.Result.Error($"Cannot check in reservation with status {reservation.Status}");

        // Get resource
        var resource = await _resourceRepository.GetByIdAsync(reservation.ResourceId, cancellationToken);
        if (resource == null)
            return Ardalis.Result.Result.NotFound("Resource not found");

        // Validate resource type allows check-in
        if (resource.ResourceTypeCode != "OPEN_WORKSPACE" && resource.ResourceTypeCode != "CLOSED_OFFICE")
            return Ardalis.Result.Result.Error("Check-in only allowed for offices (OPEN_WORKSPACE, CLOSED_OFFICE)");

        // Validate QR matches
        if (resource.PublicQrId != request.ScannedPublicQrId)
            return Ardalis.Result.Result.Error("QR code does not match resource");

        // Validate date matches today
        if (reservation.ReservationDate != DateOnly.FromDateTime(DateTime.Today))
            return Ardalis.Result.Result.Error("Reservation is not for today");

        // Validate within time window
        var now = DateTimeOffset.Now;
        var reservationStart = new DateTimeOffset(reservation.ReservationDate.ToDateTime(reservation.StartTime));
        var reservationEnd = new DateTimeOffset(reservation.ReservationDate.ToDateTime(reservation.EndTime));

        if (now < reservationStart.AddMinutes(-15))
            return Ardalis.Result.Result.Error("Check-in not allowed before reservation start (15 min grace period)");

        if (now > reservationEnd.AddMinutes(15))
            return Ardalis.Result.Result.Error("Check-in not allowed after reservation end (15 min grace period)");

        // Create check-in
        var checkInResult = CheckIn.Create(
            reservation.Id,
            reservation.ResourceId,
            userId,
            request.ScannedPublicQrId);

        if (!checkInResult.IsSuccess)
            return Ardalis.Result.Result.Error(checkInResult.Error.Message);

        var checkIn = checkInResult.Value;
        await _checkInRepository.AddAsync(checkIn, cancellationToken);

        // Update reservation status
        var checkInResult2 = reservation.CheckIn(userId, checkIn.ScannedPublicQrId.ToString());
        if (!checkInResult2.IsSuccess)
            return Ardalis.Result.Result.Error(checkInResult2.Error.Message);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ardalis.Result.Result.Success(new CheckInDto(
            checkIn.Id,
            checkIn.ReservationId,
            checkIn.ResourceId,
            checkIn.UserId,
            checkIn.CheckedInAt,
            checkIn.IpAddress,
            checkIn.UserAgent));
    }
}