# ADR-0012: Result Pattern for Error Handling

## Status
Accepted

## Context
Error handling requirements:
- Explicit success/failure without exceptions for control flow
- Rich error information (codes, messages, validation details)
- Consistent across Application layer (Use Cases) and API layer
- Mapping to HTTP status codes (RFC 7807 ProblemDetails)
- No try/catch for business logic flow

## Decision
Use **Ardalis.Result** (Result Pattern) throughout Application layer, map to **RFC 7807 ProblemDetails** in API controllers.

### Result Pattern

```csharp
// Ardalis.Result provides:
Result<T>              // Success with value, or Failure with errors
Result                 // Success/Failure without value

// Usage in Use Case Handlers
public async Task<Result<ReservationDto>> Handle(CreateReservationCommand cmd, CancellationToken ct)
{
    // Validation
    var validation = await _validator.ValidateAsync(cmd, ct);
    if (!validation.IsValid)
        return Result.Invalid(validation.ToValidationErrors());

    // Business Rule Checks
    var canReserve = await _policyService.CanReserveAsync(cmd.UserId, cmd.ResourceTypeCode, ct);
    if (!canReserve)
        return Result.Forbidden("User not authorized to reserve this resource type");

    var hasCapacity = await _availabilityService.HasAvailabilityAsync(cmd.ResourceId, cmd.Date, cmd.Start, cmd.End, ct);
    if (!hasCapacity)
        return Result.Conflict("Resource not available for selected time slot");

    // Future reservation limit
    var futureCount = await _reservationRepo.CountFutureActiveAsync(cmd.UserId, ct);
    var limit = await _settings.GetMaxFutureReservationsAsync(ct);
    if (futureCount >= limit && !await _exceptionService.HasExceptionAsync(cmd.UserId, cmd.ResourceTypeCode, ct))
        return Result.Error($"Maximum {limit} future reservations exceeded");

    // Create
    var reservation = Reservation.Create(...);
    await _repository.AddAsync(reservation, ct);
    await _unitOfWork.SaveChangesAsync(ct);

    return Result.Success(reservation.ToDto());
}
```

### Controller Mapping

```csharp
// Base Controller with Result → ActionResult extension
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<T> ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        
        return result.Status switch
        {
            ResultStatus.Invalid => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest)),
            ResultStatus.NotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound)),
            ResultStatus.Forbidden => Forbid(), // or 403 with ProblemDetails
            ResultStatus.Unauthorized => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized)),
            ResultStatus.Conflict => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict)),
            ResultStatus.Error => Problem(CreateProblemDetails(result, StatusCodes.Status500InternalServerError)),
            _ => Problem(CreateProblemDetails(result, StatusCodes.Status500InternalServerError))
        };
    }

    private ProblemDetails CreateProblemDetails(Result result, int statusCode)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(result.Status),
            Detail = result.Errors.FirstOrDefault()?.Message ?? "An error occurred",
            Type = $"https://booking.company.com/errors/{result.Status.ToString().ToLower()}",
            Instance = HttpContext.Request.Path,
            Extensions = 
            {
                ["traceId"] = HttpContext.TraceIdentifier,
                ["errors"] = result.ValidationErrors.Select(e => new { e.Identifier, e.ErrorMessage }).ToArray()
            }
        };
    }
}
```

### ProblemDetails Response Example

```json
{
  "type": "https://booking.company.com/errors/conflict",
  "title": "Conflict",
  "status": 409,
  "detail": "Resource not available for selected time slot",
  "instance": "/api/v1/reservations",
  "traceId": "00-abc123-...",
  "errors": []
}
```

### Validation Errors (400)

```json
{
  "type": "https://booking.company.com/errors/invalid",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "instance": "/api/v1/reservations",
  "traceId": "00-abc123-...",
  "errors": [
    { "identifier": "StartTime", "errorMessage": "Start time must be before end time" },
    { "identifier": "ResourceId", "errorMessage": "Resource is required" }
  ]
}
```

## Consequences

### Positive
- **Explicit Errors**: Callers must handle success/failure (no hidden exceptions)
- **Rich Information**: Error codes, messages, validation details structured
- **HTTP Mapping**: Clean mapping to RFC 7807 standard
- **Testability**: Easy to assert Result.IsSuccess / .IsFailure in unit tests
- **No Exception Overhead**: Business logic uses Result, not try/catch

### Negative
- **Boilerplate**: Result<T> return types everywhere
- **Learning Curve**: Team must adopt Result pattern thinking
- **Interop**: Libraries throwing exceptions need wrapping

### Neutral
- Ardalis.Result chosen over OneOf/Optional for richer error metadata
- Exceptions still used for truly exceptional cases (DB connection lost, config missing)

## Alternatives Considered

1. **Exceptions for Control Flow**
   - Rejected: Hidden control flow, performance overhead, hard to test

2. **Custom Result<T> (No Library)**
   - Rejected: Reinventing wheel, Ardalis.Result is battle-tested

3. **OneOf<T, Error> / LanguageExt**
   - Rejected: Less ergonomic for validation errors, no ProblemDetails mapping

4. **FluentResults**
   - Rejected: Similar to Ardalis, but Ardalis has better ProblemDetails integration

## References
- [Ardalis.Result](https://github.com/ardalis/Result)
- [RFC 7807 ProblemDetails](https://datatracker.ietf.org/doc/html/rfc7807)
- [ASP.NET Core ProblemDetails](https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors)
- [Result Pattern by Vladimir Khorikov](https://enterprisecraftsmanship.com/posts/functional-csharp-error-handling-result-pattern/)