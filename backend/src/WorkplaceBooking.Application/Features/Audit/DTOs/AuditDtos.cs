using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.Features.Audit.DTOs;

public record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string ActorEmail,
    string Action,
    string EntityName,
    Guid? EntityId,
    string? BeforeValue,
    string? AfterValue,
    string? Reason,
    string? IpAddress,
    string? UserAgent,
    Guid? CorrelationId,
    DateTimeOffset CreatedAt);

public record AuditLogQueryDto(
    int Page = 1,
    int PageSize = 20,
    Guid? ActorUserId = null,
    string? Action = null,
    string? EntityName = null,
    Guid? EntityId = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null) : IRequest<Result<PagedResult<AuditLogDto>>>;