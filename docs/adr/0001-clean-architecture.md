# ADR-0001: Clean Architecture Layering

## Status
Accepted

## Context
We need an architecture that:
- Separates business logic from infrastructure/frameworks
- Enables unit testing of core domain rules without external dependencies
- Allows swapping implementations (DB, Email, Auth) without touching business logic
- Supports team scaling with clear ownership boundaries
- Follows industry best practices for maintainable .NET/React applications

## Decision
Adopt Clean Architecture with four concentric layers:

```
Domain (innermost)
    ↑
Application
    ↑
Infrastructure
    ↑
Presentation (API + Frontend)
```

### Layer Responsibilities

| Layer | Project | Responsibilities | Dependencies |
|-------|---------|------------------|--------------|
| Domain | `BookingPlatform.Domain` | Entities, Value Objects, Domain Events, Domain Services, Repository Interfaces, Specifications, Exceptions | None (pure C#/TS) |
| Application | `BookingPlatform.Application` | Use Cases (Commands/Queries), DTOs, Validators, Mapping Profiles, Application Service Interfaces | Domain |
| Infrastructure | `BookingPlatform.Infrastructure` | EF Core Repositories, DbContext, Migrations, Entra ID Auth, Email Service, Background Workers, External APIs | Domain, Application |
| Presentation | `BookingPlatform.Api` + `Frontend` | Controllers, Middleware, Filters, React Components, Pages, Hooks | Application |

### Dependency Rule
Dependencies point **inward only**. Inner layers know nothing of outer layers.

## Consequences

### Positive
- **Testability**: Domain and Application logic unit-testable in isolation (no DB, no HTTP)
- **Maintainability**: Clear separation, changes to infrastructure don't leak to business logic
- **Flexibility**: Swap PostgreSQL → SQL Server, SMTP → SendGrid, Entra ID → Auth0 without touching Domain/Application
- **Team Autonomy**: Teams can own layers with well-defined contracts
- **Framework Independence**: Business logic not coupled to ASP.NET Core, EF Core, React, MUI

### Negative
- **Initial Complexity**: More projects, more boilerplate, steeper learning curve
- **Mapping Overhead**: DTOs ↔ Entities mapping required (AutoMapper helps)
- **Performance**: Additional abstraction layers (negligible in practice)

### Neutral
- Requires discipline to maintain dependency direction
- Code reviews must enforce architectural boundaries

## Alternatives Considered

1. **Traditional 3-Layer (Controller → Service → Repository)**
   - Rejected: Tight coupling to EF Core in services, hard to test, business logic leaks into controllers

2. **Vertical Slice Architecture (Minimal APIs + MediatR)**
   - Rejected: Good for small apps, but less structure for team scaling and shared domain logic

3. **Modular Monolith with Domain-Driven Design**
   - Considered: Similar benefits, but Clean Architecture provides clearer layer boundaries for this team size

## References
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)
- [Microsoft Clean Architecture eBook](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)