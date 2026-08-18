using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;

namespace WorkplaceBooking.Application.UseCases.Commands.Reservations;

public record CreateReservationCommand(
    Guid ResourceId,
    DateOnly ReservationDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Title = null,
    string? Description = null,
    int? AttendeeCount = null) : IRequest<Result<ReservationDto>>;

public record UpdateReservationCommand(
    Guid ReservationId,
    DateOnly? ReservationDate = null,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    string? Title = null,
    string? Description = null,
    int? AttendeeCount = null,
    string? SupportChangeReason = null) : IRequest<Result<ReservationDto>>;

public record CancelReservationCommand(
    Guid ReservationId,
    string? Reason = null) : IRequest<Result>;

public record CheckInReservationCommand(
    Guid ReservationId,
    Guid ScannedPublicQrId) : IRequest<Result<CheckInDto>>;

public record CheckOutReservationCommand(
    Guid ReservationId) : IRequest<Result>;