using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Application.Interfaces;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;

namespace WorkplaceBooking.Application.UseCases.Commands.Reservations;

public class CheckInReservationHandler : IRequestHandler<CheckInReservationCommand, Result<CheckInDto>>
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

    public async Task<Result<CheckInDto>> Handle(CheckInReservationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation == null)
            return Result.NotFound("Reservation not found");

        // Check ownership
        if (reservation.UserId != userId)
            return Result.Forbidden("Only reservation owner can check in");

        // Validate status
        if (reservation.Status != ReservationStatus.CONFIRMED)
            return Result.Error($"Cannot check in reservation with status {reservation.Status}");

        // Get resource
        var resource = await _resourceRepository.GetByIdAsync(reservation.ResourceId, cancellationToken);
        if (resource == null)
            return Result.NotFound("Resource not found");

        // Validate resource type allows check-in
        if (resource.ResourceTypeCode != "OPEN_WORKSPACE" && resource.ResourceTypeCode != "CLOSED_OFFICE")
            return Result.Error("Check-in only allowed for offices (OPEN_WORKSPACE, CLOSED_OFFICE)");

        // Validate QR matches
        if (resource.PublicQrId != request.ScannedPublicQrId)
            return Result.Error("QR code does not match resource");

        // Validate date matches today
        if (reservation.ReservationDate != DateOnly.FromDateTime(DateTime.Today))
            return Result.Error("Reservation is not for today");

        // Validate within time window
        var now = DateTimeOffset.Now;
        var reservationStart = new DateTimeOffset(reservation.ReservationDate.ToDateTime(reservation.StartTime));
        var reservationEnd = new DateTimeOffset(reservation.ReservationDate.ToDateTime(reservation.EndTime));

        if (now < reservationStart.AddMinutes(-15))
            return Result.Error("Check-in not allowed before reservation start (15 min grace period)");

        if (now > reservationEnd.AddMinutes(15))
            return Result.Error("Check-in not allowed after reservation end (15 min grace period)");

        // Create check-in
        var checkInResult = CheckIn.Create(
            reservation.Id,
            reservation.ResourceId,
            userId,
            request.ScannedPublicQrId);

        if (!checkInResult.IsSuccess)
            return Result.Error(checkInResult.Errors.First().Message);

        var checkIn = checkInResult.Value;
        await _checkInRepository.AddAsync(checkIn, CancellationToken.None);

        // Update reservation status
        var checkInResult2 = reservation.CheckIn(userId, checkIn.ScannedPublicQrId.ToString());
        if (!checkInResult2.IsSuccess)
            return Result.Error(checkInResult2.Errors.First().Message);

        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        return Result.Success(new CheckInDto(
            checkIn.Id,
            checkIn.ReservationId,
            checkIn.ResourceId,
            checkIn.UserId,
            checkIn.CheckedInAt,
            checkIn.IpAddress,
            checkIn.UserAgent));
    }
}