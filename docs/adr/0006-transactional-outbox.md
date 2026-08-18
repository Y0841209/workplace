# ADR-0006: Transactional Outbox Pattern for Notifications

## Status
Accepted

## Context
Notification requirements:
- Email on reservation create/modify/cancel/reminder
- Must be reliable (no lost emails)
- Must not block API response (async)
- Must survive process crashes/restarts
- Retry logic with backoff for transient failures
- Audit trail of notification attempts

Direct SMTP in request handler violates all of the above.

## Decision
Implement **Transactional Outbox Pattern**: Write notifications to `notification_outbox` table in same transaction as business event. Background worker polls and sends.

### Database Schema

```sql
CREATE TABLE notification_outbox (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reservation_id uuid REFERENCES reservations(id),
    recipient_user_id uuid NOT NULL REFERENCES app_users(id),
    recipient_email citext NOT NULL,
    type notification_type NOT NULL,  -- ENUM: CREATED, MODIFIED, CANCELLED, REMINDER
    subject text NOT NULL,
    body text NOT NULL,
    scheduled_at timestamptz NOT NULL DEFAULT now(),
    sent_at timestamptz,
    status notification_status NOT NULL DEFAULT 'PENDING',  -- PENDING, SENT, FAILED, CANCELLED
    retry_count int NOT NULL DEFAULT 0,
    last_error text,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_notification_pending ON notification_outbox(status, scheduled_at) 
WHERE status = 'PENDING';
```

### Domain Event → Outbox

```csharp
// In Use Case Handler (same transaction)
public async Task<Result<ReservationDto>> Handle(CreateReservationCommand cmd, CancellationToken ct)
{
    var reservation = Reservation.Create(...);
    await _repository.AddAsync(reservation, ct);
    
    // Raise domain event - captured by EF Core interceptor
    reservation.RaiseEvent(new ReservationCreatedEvent(reservation));
    
    await _unitOfWork.SaveChangesAsync(ct);
    return reservation.ToDto();
}

// EF Core Interceptor (SaveChangesInterceptor)
public override InterceptionResult<int> SavingChanges(
    DbContextEventData eventData, InterceptionResult<int> result)
{
    var outboxMessages = eventData.Context.ChangeTracker
        .Entries<IAggregateRoot>()
        .SelectMany(e => e.Entity.DomainEvents)
        .Select(MapToOutboxMessage);
    
    eventData.Context.Set<NotificationOutbox>().AddRange(outboxMessages);
    return base.SavingChanges(eventData, result);
}
```

### Background Worker (Hangfire)

```csharp
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 1800 })]
public class NotificationProcessor
{
    private readonly IEmailService _email;
    private readonly IUnitOfWork _uow;
    
    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        var pending = await _uow.Notifications.GetPendingAsync(ct); // scheduled_at <= NOW
        
        foreach (var msg in pending)
        {
            try
            {
                await _email.SendAsync(msg.RecipientEmail, msg.Subject, msg.Body, ct);
                msg.MarkSent();
            }
            catch (Exception ex)
            {
                msg.MarkFailed(ex.Message);
            }
        }
        await _uow.SaveChangesAsync(ct);
    }
    
    // Scheduled: Every minute for immediate, plus dedicated reminder job
    [Queue("notifications")]
    public Task ProcessImmediateAsync() => ProcessPendingAsync();
    
    [Queue("reminders")]
    [Cron("*/15 * * * *")] // Every 15 minutes
    public Task ProcessRemindersAsync() => ProcessPendingAsync(); // Filters REMINDER type
}
```

### Retry Logic
- **Immediate**: Retry 1 min, 5 min, 30 min (exponential)
- **Reminder**: Dedicated job runs every 15 min, processes `scheduled_at <= NOW`
- **Dead Letter**: After 3 failures → `status = FAILED`, alert admin

## Consequences

### Positive
- **Reliability**: Notification persisted with business event (atomic)
- **No Lost Emails**: Survives crashes, restarts, deployments
- **Async**: API returns immediately, email sent in background
- **Observability**: Full audit of every notification attempt
- **Retry**: Configurable backoff, dead-letter tracking
- **Scalability**: Worker independent, can scale horizontally

### Negative
- **Eventual Consistency**: Email sent seconds/minutes after event
- **Complexity**: Additional table, worker, monitoring
- **Duplicate Risk**: At-least-once delivery (idempotency keys needed for critical)

### Neutral
- Requires Hangfire (or similar) for scheduling/persistence
- Email templates stored in code or database

## Alternatives Considered

1. **Direct SMTP in Handler**
   - Rejected: Blocks request, fails if SMTP down, no retry, no audit

2. **Message Queue (RabbitMQ / Azure Service Bus)**
   - Rejected: Additional infrastructure, dual-write problem (DB + Queue)

3. **CDC (Change Data Capture) → Event Processor**
   - Rejected: Overkill, complex, eventual consistency anyway

4. **Transactional Messaging (MassTransit Outbox)**
   - Rejected: Adds MassTransit dependency; simple table + worker sufficient

## References
- [Transactional Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Hangfire Documentation](https://www.hangfire.io/)
- [EF Core Interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)