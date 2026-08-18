# ADR-0014: Hangfire for Background Job Processing

## Status
Accepted

## Context
Background job requirements:
- Email notifications (transactional outbox processor)
- Reservation reminders (15 min before start)
- QR code rotation / cleanup
- Audit log archival
- Recurring scheduled jobs (daily, hourly)
- Persistence across restarts/deployments
- Monitoring and retry dashboard
- Manual job triggering for admin operations

## Decision
Use **Hangfire** with PostgreSQL storage for background job processing.

### Why Hangfire

| Requirement | Hangfire Solution |
|-------------|-------------------|
| Persistence | PostgreSQL storage (jobs survive restarts) |
| Scheduling | Cron expressions, delayed jobs, recurring |
| Retries | Automatic with configurable policy |
| Dashboard | Built-in `/hangfire` monitoring UI |
| Scaling | Multiple workers, same storage |
| .NET Integration | Native, DI-friendly, async support |

### Configuration

```csharp
// Program.cs
builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("Hangfire"))
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .WithJobExpirationTimeout(TimeSpan.FromDays(30))
);

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "critical", "default", "notifications", "reminders", "maintenance" };
    options.ServerName = $"booking-api-{Environment.MachineName}";
});

// Dashboard (protected by GLOBAL_ADMIN policy)
builder.Services.AddHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthorizationFilter()],
    DashboardTitle = "Booking Platform Jobs",
    StatsPollingInterval = 30000,
});
```

### Job Definitions

```csharp
// Notification Processor (Queue: notifications)
public class NotificationProcessor
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;

    [Queue("notifications")]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 1800 })]
    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        var pending = await _uow.Notifications.GetPendingAsync(ct);
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
}

// Reminder Processor (Queue: reminders, Cron: every 15 min)
public class ReminderProcessor
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;

    [Queue("reminders")]
    [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 60, 120, 300 })]
    public async Task ProcessRemindersAsync(CancellationToken ct)
    {
        var windowStart = DateTimeOffset.UtcNow;
        var windowEnd = windowStart.AddMinutes(20); // 15 min before + buffer
        
        var upcoming = await _uow.Reservations.GetUpcomingForReminderAsync(windowStart, windowEnd, ct);
        
        foreach (var reservation in upcoming)
        {
            var notification = new NotificationOutbox
            {
                ReservationId = reservation.Id,
                RecipientUserId = reservation.UserId,
                RecipientEmail = reservation.User.Email,
                Type = NotificationType.ReservationReminder,
                Subject = $"Recordatorio: Reserva en 15 minutos - {reservation.Resource.Name}",
                Body = BuildReminderBody(reservation),
                ScheduledAt = DateTimeOffset.UtcNow
            };
            
            _uow.Notifications.Add(notification);
        }
        await _uow.SaveChangesAsync(ct);
    }
}

// Recurring Job Registration (Startup)
public class HangfireJobRegistrar
{
    public static void RegisterRecurringJobs(IRecurringJobManager manager)
    {
        // Every minute: Process immediate notifications
        manager.AddOrUpdate<NotificationProcessor>(
            "process-notifications",
            p => p.ProcessPendingAsync(default),
            Cron.MinuteInterval(1),
            new RecurringJobOptions { QueueName = "notifications" }
        );

        // Every 15 minutes: Check for reminders
        manager.AddOrUpdate<ReminderProcessor>(
            "process-reminders",
            p => p.ProcessRemindersAsync(default),
            "*/15 * * * *",
            new RecurringJobOptions { QueueName = "reminders" }
        );

        // Daily 02:00: Cleanup old notifications
        manager.AddOrUpdate<MaintenanceProcessor>(
            "cleanup-notifications",
            p => p.CleanupOldNotificationsAsync(default),
            "0 2 * * *",
            new RecurringJobOptions { QueueName = "maintenance" }
        );

        // Weekly Sunday 03:00: Archive audit logs
        manager.AddOrUpdate<MaintenanceProcessor>(
            "archive-audit-logs",
            p => p.ArchiveAuditLogsAsync(default),
            "0 3 * * 0",
            new RecurringJobOptions { QueueName = "maintenance" }
        );
    }
}
```

### Worker Deployment

```yaml
# docker-compose.yml (worker service)
worker:
  build:
    context: .
    dockerfile: src/backend/src/BookingPlatform.Api/Dockerfile
  command: ["dotnet", "BookingPlatform.Api.dll", "--worker"]
  environment:
    - ASPNETCORE_ENVIRONMENT=Production
    - ConnectionStrings__DefaultConnection=Host=postgres;Database=booking;...
    - ConnectionStrings__Hangfire=Host=postgres;Database=booking;...
  deploy:
    replicas: 2
    resources:
      limits:
        memory: 512M
```

```csharp
// Program.cs - Worker Mode
if (args.Contains("--worker"))
{
    // Run only Hangfire server, no HTTP endpoints
    var host = CreateHostBuilder(args).Build();
    host.Run(); // HangfireServer runs as hosted service
    return;
}
```

## Consequences

### Positive
- **Reliability**: Jobs persisted in PostgreSQL, survive crashes/restarts
- **Observability**: Built-in dashboard for monitoring, retrying, debugging
- **Flexibility**: Fire-and-forget, delayed, recurring, cron all supported
- **Scalability**: Add worker replicas, same storage
- **Integration**: Native .NET DI, async/await, cancellation tokens

### Negative
- **Additional Dependency**: Hangfire + PostgreSQL storage
- **Schema**: Creates own tables in same DB (hangfire.* schema)
- **Dashboard Security**: Must protect with authorization
- **Polling Overhead**: Workers poll DB (configurable interval)

### Neutral
- Alternative: .NET Hosted Services + custom scheduler (rejected: no persistence, no dashboard)
- Alternative: Quartz.NET (rejected: more complex, no built-in dashboard)
- Alternative: MassTransit + RabbitMQ (rejected: additional infrastructure)

## Alternatives Considered

1. **Quartz.NET**
   - Rejected: No built-in dashboard, more configuration, similar features

2. **MassTransit + RabbitMQ**
   - Rejected: Additional message broker infrastructure

3. **Custom Hosted Services + Timer**
   - Rejected: No persistence, no retries, no visibility

4. **Azure Functions / AWS Lambda**
   - Rejected: Vendor lock-in, cold starts, cost at scale

## References
- [Hangfire Documentation](https://docs.hangfire.io/)
- [Hangfire PostgreSQL Storage](https://github.com/HangfireIO/Hangfire.PostgreSql)
- [Background Tasks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)