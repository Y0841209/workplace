# ADR-0002: CQRS with MediatR for Backend

## Status
Accepted

## Context
The backend needs to handle:
- Complex business rules for reservations (conflicts, limits, permissions)
- Different read/write models (search availability vs create reservation)
- Cross-cutting concerns (validation, logging, transactions, audit)
- Clear separation between commands (mutations) and queries (reads)
- Testability of use cases in isolation

## Decision
Implement CQRS pattern using **MediatR** as the in-process mediator with pipeline behaviors.

### Structure

```
BookingPlatform.Application/
├── UseCases/
│   ├── Commands/          # Write operations (return Result<T>)
│   │   ├── CreateReservationCommand
│   │   ├── ModifyReservationCommand
│   │   ├── CancelReservationCommand
│   │   └── CheckInCommand
│   └── Queries/           # Read operations (return Result<T>)
│       ├── GetAvailableResourcesQuery
│       ├── GetMyReservationsQuery
│       └── GetAuditLogsQuery
├── Behaviors/             # Pipeline behaviors (cross-cutting)
│   ├── ValidationBehavior
│   ├── LoggingBehavior
│   ├── TransactionBehavior
│   └── AuditBehavior
└── Common/
    ├── Result.cs          # Ardalis.Result wrapper
    └── Pagination.cs
```

### Controller Pattern
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(
        CreateReservationCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult(); // Maps Result → HTTP status
    }
}
```

## Consequences

### Positive
- **Single Responsibility**: Each handler does one thing
- **Testability**: Handlers tested with pure unit tests (mock dependencies)
- **Cross-Cutting**: Validation, logging, transactions via behaviors (DRY)
- **Explicit Contracts**: Command/Query types = living documentation
- **Extensibility**: New behaviors added without touching handlers

### Negative
- **Boilerplate**: More classes than traditional service layer
- **Indirection**: Extra hop through mediator (negligible)
- **Learning Curve**: Team must understand CQRS + MediatR patterns

### Neutral
- Queries can use different read models (Dapper/Views) if needed later
- Commands enforce intent-revealing naming

## Alternatives Considered

1. **Traditional Service Layer (`IReservationService`)**
   - Rejected: Mixes read/write, harder to apply cross-cutting concerns uniformly

2. **Minimal APIs with Vertical Slices (no MediatR)**
   - Rejected: Less structure for shared behaviors, duplicate validation/logging

3. **Full CQRS with Separate Read/Write Databases**
   - Rejected: Overkill for this domain; single PostgreSQL with optimized indexes sufficient

## References
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [CQRS by Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [Ardalis.Result for Result Pattern](https://github.com/ardalis/Result)