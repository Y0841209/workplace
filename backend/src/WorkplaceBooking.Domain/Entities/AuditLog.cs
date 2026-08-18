namespace WorkplaceBooking.Domain.Entities;

public class AuditLog : Entity, IAuditableEntity
{
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string? BeforeValue { get; private set; }
    public string? AfterValue { get; private set; }
    public string? Reason { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public Guid? CorrelationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public AppUser? ActorUser { get; private set; }

    private AuditLog() { }

    private AuditLog(
        Guid id,
        Guid? actorUserId,
        string action,
        string entityName,
        Guid? entityId,
        string? beforeValue,
        string? afterValue,
        string? reason,
        string? ipAddress,
        string? userAgent,
        Guid? correlationId)
        : base(id)
    {
        ActorUserId = actorUserId;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        BeforeValue = beforeValue;
        AfterValue = afterValue;
        Reason = reason;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static AuditLog Create(
        Guid? actorUserId,
        string action,
        string entityName,
        Guid? entityId,
        string? beforeValue,
        string? afterValue,
        string? reason,
        string? ipAddress,
        string? userAgent,
        Guid? correlationId)
    {
        return new AuditLog(Guid.NewGuid(), actorUserId, action, entityName, entityId, beforeValue, afterValue, reason, ipAddress, userAgent, correlationId);
    }
}
{
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string? BeforeValue { get; private set; }
    public string? AfterValue { get; private set; }
    public string? Reason { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public Guid? CorrelationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation
    public AppUser? ActorUser { get; private set; }

    private AuditLog() { }

    private AuditLog(
        Guid id,
        Guid? actorUserId,
        string action,
        string entityName,
        Guid? entityId,
        string? beforeValue,
        string? afterValue,
        string? reason,
        string? ipAddress,
        string? userAgent,
        Guid? correlationId)
        : base(id)
    {
        ActorUserId = actorUserId;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        BeforeValue = beforeValue;
        AfterValue = afterValue;
        Reason = reason;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static AuditLog Create(
        Guid? actorUserId,
        string action,
        string entityName,
        Guid? entityId,
        string? beforeValue,
        string? afterValue,
        string? reason,
        string? ipAddress,
        string? userAgent,
        Guid? correlationId)
    {
        return new AuditLog(Guid.NewGuid(), actorUserId, action, entityName, entityId, beforeValue, afterValue, reason, ipAddress, userAgent, correlationId);
    }
}