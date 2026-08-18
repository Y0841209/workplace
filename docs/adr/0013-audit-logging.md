# ADR-0013: Dual Audit Logging (Middleware + Domain Events)

## Status
Accepted

## Context
Audit requirements:
- All mutating HTTP requests logged (POST, PUT, DELETE, PATCH)
- Business-critical actions with rich context (before/after values, reason)
- Immutable, append-only, queryable
- Correlation IDs for distributed tracing
- Actor identification (user, IP, user agent)
- Compliance: legal firm, regulatory audit trail

## Decision
**Dual Audit Strategy**: HTTP Middleware for all requests + Explicit Domain Events for business actions.

### 1. HTTP Middleware (Infrastructure Layer)

```csharp
// AuditMiddleware.cs
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditLogger _auditLogger;

    public async Task InvokeAsync(HttpContext context)
    {
        // Only audit mutating requests
        if (!IsMutatingMethod(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                           ?? Guid.NewGuid().ToString();
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var originalBody = context.Response.Body;
        await using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        var startTime = DateTimeOffset.UtcNow;
        Exception? exception = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            var elapsed = DateTimeOffset.UtcNow - startTime;
            
            // Capture request body (if not too large)
            string requestBody = await GetRequestBodyAsync(context.Request);
            
            // Capture response body
            memoryStream.Position = 0;
            string responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBody);

            var auditEntry = new AuditLogEntry
            {
                CorrelationId = Guid.Parse(correlationId),
                ActorUserId = GetUserId(context.User),
                Action = $"{context.Request.Method} {context.Request.Path}",
                EntityName = ExtractEntityName(context.Request.Path),
                EntityId = ExtractEntityId(context.Request.Path),
                BeforeValue = null, // Middleware doesn't know before state
                AfterValue = ParseJsonOrNull(responseBody),
                Reason = null,
                IpAddress = GetClientIp(context),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                StatusCode = context.Response.StatusCode,
                Exception = exception?.ToString(),
                DurationMs = (long)elapsed.TotalMilliseconds,
                CreatedAt = startTime
            };

            await _auditLogger.LogAsync(auditEntry);
        }
    }
}
```

### 2. Domain Events (Application/Domain Layer)

```csharp
// Domain Event
public record ReservationCreatedEvent(
    Guid ReservationId,
    Guid ResourceId,
    Guid UserId,
    DateOnly ReservationDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Title
) : IDomainEvent;

// Entity raises event
public class Reservation : AggregateRoot
{
    public static Result<Reservation> Create(...)
    {
        var reservation = new Reservation(...);
        reservation.RaiseEvent(new ReservationCreatedEvent(...));
        return Result.Success(reservation);
    }
}

// EF Core Interceptor captures events
public class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IAuditLogger _auditLogger;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct)
    {
        var entitiesWithEvents = eventData.Context!.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        foreach (var entityEntry in entitiesWithEvents)
        {
            foreach (var domainEvent in entityEntry.Entity.DomainEvents)
            {
                await _auditLogger.LogDomainEventAsync(domainEvent, entityEntry.Entity, ct);
            }
            entityEntry.Entity.ClearDomainEvents();
        }

        return await base.SavingChangesAsync(eventData, result, ct);
    }
}

// AuditLogger Implementation
public class AuditLogger : IAuditLogger
{
    private readonly BookingPlatformDbContext _dbContext;

    public async Task LogDomainEventAsync(IDomainEvent domainEvent, IAggregateRoot entity, CancellationToken ct)
    {
        var entry = new AuditLog
        {
            ActorUserId = GetCurrentUserId(), // From ICurrentUserService
            Action = domainEvent.GetType().Name.Replace("Event", ""),
            EntityName = entity.GetType().Name,
            EntityId = entity.Id,
            BeforeValue = GetBeforeState(entity), // From change tracker
            AfterValue = GetAfterState(entity),
            Reason = domainEvent switch
            {
                ReservationCancelledEvent e => e.Reason,
                ReservationModifiedBySupportEvent e => e.Reason,
                _ => null
            },
            IpAddress = GetCurrentIp(),
            UserAgent = GetCurrentUserAgent(),
            CorrelationId = GetCurrentCorrelationId(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.AuditLogs.Add(entry);
        // Saved in same transaction as business entity
    }
}
```

### Audit Log Schema

```sql
CREATE TABLE audit_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    actor_user_id uuid REFERENCES app_users(id),
    action text NOT NULL,
    entity_name text NOT NULL,
    entity_id uuid,
    before_value jsonb,
    after_value jsonb,
    reason text,
    ip_address inet,
    user_agent text,
    correlation_id uuid,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_audit_logs_actor ON audit_logs(actor_user_id, created_at DESC);
CREATE INDEX ix_audit_logs_entity ON audit_logs(entity_name, entity_id);
CREATE INDEX ix_audit_logs_action ON audit_logs(action, created_at DESC);
CREATE INDEX ix_audit_logs_correlation ON audit_logs(correlation_id);
```

## Consequences

### Positive
- **Complete Coverage**: Middleware catches ALL mutations, domain events add business context
- **Rich Context**: Domain events include before/after, reason, actor
- **Transactional**: Domain event audit saved in same transaction as entity
- **Immutable**: Append-only table, never updated/deleted
- **Queryable**: Indexed by actor, entity, action, correlation ID
- **Compliance Ready**: Meets legal/financial audit requirements

### Negative
- **Storage Growth**: Audit logs accumulate (implement retention policy)
- **Performance**: Middleware captures request/response bodies (limit size)
- **Complexity**: Two systems to maintain, ensure consistency

### Neutral
- Retention policy: Archive > 2 years to cold storage
- Sensitive data (PII) masking in BeforeValue/AfterValue
- Correlation ID flows: Frontend → Nginx → API → Worker → DB

## Alternatives Considered

1. **Triggers Only (Database-Level)**
   - Rejected: No HTTP context (IP, UA, correlation ID), no business reason

2. **CDC (Change Data Capture) → Kafka → Audit Store**
   - Rejected: Overkill, eventual consistency, complex

3. **Middleware Only**
   - Rejected: No before/after values, no business reason, no domain context

4. **Domain Events Only**
   - Rejected: Misses failed requests, validation errors, non-domain mutations

## References
- [Audit Logging Patterns](https://martinfowler.com/articles/audit-logging.html)
- [EF Core Interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [Correlation IDs](https://microservices.io/patterns/observability/correlation-id.html)