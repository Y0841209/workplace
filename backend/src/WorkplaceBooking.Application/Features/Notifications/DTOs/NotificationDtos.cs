using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.DTOs;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.Features.Notifications.DTOs;

public record NotificationDto(
    Guid Id,
    Guid? ReservationId,
    Guid RecipientUserId,
    string RecipientEmail,
    string Type,
    string Subject,
    string Body,
    DateTimeOffset ScheduledAt,
    DateTimeOffset? SentAt,
    string Status,
    int RetryCount,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record NotificationQueryDto(
    int Page = 1,
    int PageSize = 20,
    string? Type = null,
    string? Status = null,
    DateTimeOffset? ScheduledFrom = null,
    DateTimeOffset? ScheduledTo = null) : IRequest<Ardalis.Result.Result<WorkplaceBooking.Application.Common.DTOs.PagedResult<NotificationDto>>>;