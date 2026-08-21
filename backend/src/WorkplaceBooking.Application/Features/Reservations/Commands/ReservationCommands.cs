using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Features.CheckIns.DTOs;
using WorkplaceBooking.Application.Features.Reservations.DTOs;

namespace WorkplaceBooking.Application.Features.Reservations.Commands;

public record CreateReservationCommand(
    Guid ResourceId,
    DateOnly ReservationDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Title = null,
    string? Description = null,
    int? AttendeeCount = null) : IRequest<Ardalis.Result.Result<ReservationDto>>;

public record UpdateReservationCommand(
    Guid ReservationId,
    DateOnly? ReservationDate = null,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    string? Title = null,
    string? Description = null,
    int? AttendeeCount = null,
    string? SupportChangeReason = null) : IRequest<Ardalis.Result.Result<ReservationDto>>;

public record CancelReservationCommand(
    Guid ReservationId,
    string? Reason = null) : IRequest<Ardalis.Result.Result>;

public record CheckInReservationCommand(
    Guid ReservationId,
    Guid ScannedPublicQrId) : IRequest<Ardalis.Result.Result<CheckInDto>>;

public record CheckOutReservationCommand(
    Guid ReservationId) : IRequest<Ardalis.Result.Result>;