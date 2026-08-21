# Build Fix Plan - Workplace Booking Platform

## Executive Summary
**Status**: Solution has structural issues preventing successful build
**Priority**: Critical - Multiple missing dependencies, incomplete implementations, and configuration gaps
**Estimated Fix Time**: 4-6 hours

---

## Priority 1: Critical - Missing Interfaces (Blockers)

### 1.1 Missing `IEmailService` Interface
**Location**: Missing in `WorkplaceBooking.Application.Common.Interfaces`
**Required By**: `EmailService` (Infrastructure), `NotificationOutbox` handlers
**Action Required**: Create `IEmailService.cs` in `WorkplaceBooking.Application.Common.Interfaces`

```csharp
// WorkplaceBooking.Application.Common.Interfaces/IEmailService.cs
namespace WorkplaceBooking.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
```

### 2. Missing `IUserAuthorizationService` Interface
**Status**: Interface exists in `WorkplaceBooking.Application.Common.Interfaces` ✓
**Implementation**: `UserAuthorizationService` exists in Infrastructure
**Status**: ✅ Already implemented and registered in DI

### 3. Missing `ICurrentUserService` Interface
**Location**: Defined in `WorkplaceBooking.Application.Common.Interfaces.IRepository.cs` (inline) - **WRONG LOCATION**
**Should be**: Separate file `ICurrentUserService.cs` in `WorkplaceBooking.Application.Common.Interfaces`

**Action**: Extract `ICurrentUserService` to its own file:
```csharp
// WorkplaceBooking.Application.Common.Interfaces/ICurrentUserService.cs
namespace WorkplaceBooking.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> BusinessProfiles { get; }
    bool IsInRole(string role);
    bool HasBusinessProfile(string profile);
    bool CanReserveResource(string resourceTypeCode);
}
```

---

## Priority 2: Missing Pipeline Behaviors (MediatR Pipeline)

### Missing Behavior Implementations

| Behavior | File | Status | Required By |
|----------|------|--------|-------------|
| `ValidationBehavior` | ❌ Missing | Required by DI | Pipeline registration |
| `LoggingBehavior` | ✅ Exists | Registered | ✅ OK |
| `TransactionBehavior` | ✅ Exists | Registered | ✅ OK |
| `AuditBehavior` | ❌ Incomplete | Registered | **BROKEN** |

### Required Implementations

#### 1. `ValidationBehavior<TRequest, TResponse>`
```csharp
// WorkplaceBooking.Application/Common/Behaviors/ValidationBehavior.cs
using FluentValidation;
using MediatR;

namespace WorkplaceBooking.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext(request);
        var failures = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, CancellationToken.None)));
        var failuresList = failures.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failuresList.Count != 0)
            throw new ValidationException(failuresList);

        return await next();
    }
}
```

---

## Priority 2: DI Registration Issues

### 1. Missing Interface Registrations in `Infrastructure/DependencyInjection.cs`

**Missing Registrations**:
```csharp
// Add these to Infrastructure/DependencyInjection.cs AddInfrastructure method:

// Email Service
services.AddScoped<IEmailService, EmailService>();

// User Authorization
services.AddScoped<IUserAuthorizationService, UserAuthorizationService>();

// Current User Service
services.AddScoped<ICurrentUserService, CurrentUserService>();

// Missing: IEmailService interface registration (if not present)
```

**Current Status**: Need to verify all services are registered:
- [x] `IEmailService` → `EmailService`
- [x] `IReservationPolicyService` → `ReservationPolicyService`
- [x] `IAvailabilityService` → `AvailabilityService`
- [x] `IQrValidationService` → `QrValidationService`
- [x] `IUserAuthorizationService` → `UserAuthorizationService`
- ❌ `ICurrentUserService` - **MISSING** (registered as `CurrentUserService` but interface in wrong namespace)

---

## Priority 3: Missing NuGet Packages

### API Project (`WorkplaceBooking.Api.csproj`)
| Package | Version | Status | Reason |
|---------|---------|--------|--------|
| `Microsoft.AspNetCore.OpenApi` | 8.0.x | ✅ Present | OpenAPI support |
| `Swashbuckle.AspNetCore` | 6.5.x | ✅ Present | Swagger/OpenAPI |
| `Scalar.AspNetCore` | 1.2.x | ✅ Present | API documentation UI |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.x | ✅ Present | JWT Auth |
| `Serilog.AspNetCore` | 8.x | ✅ Present | Logging |
| `Serilog.Sinks.Console` | 6.x | ✅ Present | Console logging |
| `Serilog.Sinks.File` | 6.x | ✅ Present | File logging |
| `Serilog.Enrichers.Environment` | 3.x | ✅ Present | Environment enrichment |
| `Serilog.Enrichers.Process` | 3.x | ✅ Present | Process enrichment |
| `Serilog.Enrichers.Thread` | 4.x | ✅ Present | Thread enrichment |
| `Microsoft.Extensions.Http.Polly` | 8.x | ⚠️ **MISSING** | Polly HTTP resilience |
| `AspNetCore.HealthChecks.NpgSql` | 8.x | ⚠️ **MISSING** | PostgreSQL health check |
| `AspNetCore.HealthChecks.UI.Client` | 8.x | ⚠️ **MISSING** | Health Checks UI |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.7.x | ⚠️ **MISSING** | OTLP export |
| `OpenTelemetry.Extensions.Hosting` | 1.7.x | ⚠️ **MISSING** | OTel hosting |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.7.x | ⚠️ **MISSING** | ASP.NET Core auto-instrumentation |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | 1.7.x | ⚠️ **MISSING** | EF Core instrumentation |
| `OpenTelemetry.Instrumentation.Http` | 1.7.x | ⚠️ **MISSING** | HTTP client instrumentation |
| `Scalar.AspNetCore` | 1.2.x | ✅ Present | API docs UI |
| `AspNetCore.RateLimit` | 5.x | ⚠️ **MISSING** | Rate limiting middleware |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.x | ✅ Present | JWT Auth |
| `Swashbuckle.AspNetCore` | 6.5.x | ✅ Present | Swagger/OpenAPI |

---

## Priority 3: Missing NuGet Packages by Project

### API Project (`WorkplaceBooking.Api.csproj`) - MISSING PACKAGES

| Package | Version | Required For |
|---------|---------|--------------|
| `Microsoft.Extensions.Http.Polly` | 8.x | HTTP resilience (Polly) |
| `AspNetCore.HealthChecks.NpgSql` | 8.x | PostgreSQL health check |
| `AspNetCore.HealthChecks.UI.Client` | 8.x | Health Checks UI |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.7.x | OTLP export |
| `OpenTelemetry.Extensions.Hosting` | 1.7.x | OTel hosting |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.7.x | ASP.NET Core auto-instrumentation |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | 1.7.x | EF Core tracing |
| `OpenTelemetry.Instrumentation.Http` | 1.7.x | HTTP client instrumentation |
| `AspNetCore.RateLimit` | 5.x | Rate limiting |
| `Serilog.AspNetCore` | 8.x | Serilog integration |
| `Serilog.Sinks.Console` | 6.x | Console logging |
| `Serilog.Sinks.File` | 6.x | File logging |
| `Serilog.Enrichers.Environment` | 3.x | Environment enrichment |
| `Serilog.Enrichers.Process` | 3.x | Process enrichment |
| `Serilog.Enrichers.Thread` | 4.x | Thread enrichment |

### Infrastructure Project (`WorkplaceBooking.Infrastructure.csproj`)

| Package | Version | Reason |
|---------|---------|--------|
| `Hangfire.AspNetCore` | 1.8.x | Hangfire dashboard |
| `Hangfire.PostgreSql` | 1.8.x | Hangfire PostgreSQL storage |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.x | JWT validation |
| `Microsoft.IdentityModel.Protocols.OpenIdConnect` | 7.x | OIDC protocol |
| `MailKit` | 4.x | SMTP email |
| `Hangfire.Core` | 1.8.x | Hangfire core |
| `Hangfire.PostgreSql` | 1.8.x | Hangfire PostgreSQL storage |
| `Serilog` | 4.x | Core Serilog |
| `OpenTelemetry.Api` | 1.7.x | OpenTelemetry API |
| `Ardalis.Result` | 10.x | Result pattern |
| `Ardalis.Specification` | 8.x | Specification pattern |
| `Ardalis.Specification.EntityFrameworkCore` | 8.x | EF Core spec integration |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.x | JWT validation |
| `Microsoft.IdentityModel.Protocols.OpenIdConnect` | 7.x | OIDC protocol |
| `MediatR` | 12.x | Mediator pattern |
| `MailKit` | 4.x | SMTP email |
| `Hangfire.Core` | 1.8.x | Hangfire core |
| `Hangfire.PostgreSql` | 1.8.x | Hangfire PostgreSQL storage |
| `Serilog` | 4.x | Logging |
| `OpenTelemetry.Api` | 1.7.x | OpenTelemetry API |
| `Ardalis.Result` | 10.x | Result pattern |
| `Ardalis.Specification` | 8.x | Specification pattern |
| `Ardalis.Specification.EntityFrameworkCore` | 8.x | EF Core specifications |

---

## Priority 4: Missing Test Infrastructure

### Test Projects Missing Packages

#### `WorkplaceBooking.Api.Tests.csproj`
| Missing Package | Version | Purpose |
|-----------------|---------|---------|
| `Microsoft.AspNetCore.Mvc.Testing` | 8.x | Integration testing |
| `WireMock.Net` | 1.5.x | HTTP mocking |
| `Testcontainers.PostgreSql` | 3.x | Testcontainers for PostgreSQL |
| `Respawn` | 6.x | Database reset for tests |

#### `WorkplaceBooking.Application.Tests.csproj`
| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.NET.Test.Sdk` | 17.x | Test SDK |
| `xunit` | 2.5.x | xUnit framework |
| `xunit.runner.visualstudio` | 2.5.x | VS runner |
| `Moq` | 4.20+ | Mocking |
| `FluentAssertions` | 6.12+ | Assertions |
| `coverlet.collector` | 6.x | Code coverage |
| `Microsoft.AspNetCore.Mvc.Testing` | 8.x | Integration testing |
| `WireMock.Net` | 1.5.x | HTTP mocking |

#### Missing Test Projects
- [ ] `WorkplaceBooking.Infrastructure.Tests` - **MISSING**
- [ ] `WorkplaceBooking.Api.Tests` - exists but incomplete
- [ ] `WorkplaceBooking.Domain.Tests` - exists
- [ ] `WorkplaceBooking.Application.Tests` - exists

---

## Fix Priority Order

### Phase 1: Critical - Unblock Build (Immediate)
1. ✅ **Fix `ICurrentUserService` interface location** - Move to correct namespace
2. ✅ **Register `ICurrentUserService` in DI** - Add to Infrastructure DI
3. ✅ **Add missing `ValidationBehavior`** pipeline behavior
4. **Add missing NuGet packages** to Infrastructure and API projects

### Priority 2: Infrastructure Services
1. **Register missing services** in Infrastructure DI:
   - `IEmailService` → `EmailService`
   - `IUserAuthorizationService` → `UserAuthorizationService`
   - `ICurrentUserService` → `CurrentUserService`
2. **Add missing NuGet packages** to Infrastructure project

### Priority 3: Missing NuGet Packages
1. **API Project**: Add missing health checks, OpenTelemetry, rate limiting packages
2. **Infrastructure**: Add missing EF Core, Hangfire, OpenTelemetry packages
3. **Test Projects**: Add missing test packages

### Priority 4: Test Infrastructure
1. Create missing test projects:
   - `WorkplaceBooking.Infrastructure.Tests`
   - `WorkplaceBooking.Api.Tests` (add missing packages)
2. Add test infrastructure packages (Testcontainers, WireMock, etc.)

### Priority 5: Frontend Dependencies
1. Add missing frontend dev dependencies
2. Configure Vite proxy for API
3. Add test utilities (MSW, Testing Library)

---

## Correct Order of Fixes

### Phase 1: Foundation (Must Fix First)
1. [ ] Fix `ICurrentUserService` location/namespace issue
2. ✅ Register `ICurrentUserService` in Infrastructure DI
3. ✅ Fix `CreateReservationValidator` constructor dependencies
4. ✅ Register all missing infrastructure services in DI

### Phase 2: Missing Behaviors
1. [ ] Create `ValidationBehavior` pipeline behavior
2. Verify `LoggingBehavior`, `TransactionBehavior`, `AuditBehavior` exist

### Phase 3: NuGet Packages
1. Add missing packages to API project
2. Add missing packages to Infrastructure project
3. Add test project packages

### Phase 4: Test Infrastructure
1. Create missing test projects
2. Add test dependencies
3. Configure test containers

### Phase 5: Frontend Dependencies
1. Add missing frontend packages
3. Configure Vite proxy for development

---

## Verification Commands

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build WorkplaceBooking.sln

# Run tests
dotnet test --configuration Release

# Check for warnings
dotnet build --no-restore --verbosity normal
```

---

## Expected Outcome After Fixes
- ✅ Solution builds without errors
- ✅ All DI registrations resolve correctly
- ✅ Pipeline behaviors execute in correct order
- ✅ Tests can run with proper DI
- ✅ Development authentication works without Azure AD
- ✅ All validators can resolve their dependencies

---

**Document Version**: 1.0
**Last Updated**: 2026-08-16
**Status**: Ready for implementation