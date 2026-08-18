# Technical Decisions

This document captures key technical decisions made during the architecture design of the Workplace Booking Platform.

## 1. Architecture Style: Clean Architecture

**Decision**: Use Clean Architecture with four layers (Domain, Application, Infrastructure, Presentation)

**Rationale**:
- Separation of concerns: Business logic isolated from frameworks
- Testability: Domain and Application layers unit-testable without infrastructure
- Maintainability: Clear dependency direction (inward)
- Flexibility: Swap infrastructure (DB, Email, Auth) without touching business logic

**Implementation**:
- Domain: Zero dependencies, pure C#/TypeScript
- Application: Depends only on Domain, uses MediatR for CQRS
- Infrastructure: Implements Domain/Application interfaces
- API/Frontend: Thin adapters over Application layer

## 2. Backend: .NET 8 Web API with CQRS/MediatR

**Decision**: Use MediatR for CQRS pattern with pipeline behaviors

**Rationale**:
- Clear separation of Commands (writes) and Queries (reads)
- Pipeline behaviors for cross-cutting concerns (validation, logging, transactions)
- Decouples controllers from business logic
- Enables easy testing of use cases in isolation

**Alternatives Considered**:
- Minimal APIs with vertical slices: Rejected - less structure for team scaling
- Traditional layered (Controller → Service → Repository): Rejected - tight coupling

## 3. Frontend: React + TypeScript + Material UI

**Decision**: React 18 with TypeScript, Material UI v5, TanStack Query

**Rationale**:
- **TypeScript**: Type safety across API contracts, shared types possible
- **Material UI**: Enterprise-grade components, theming, accessibility built-in
- **TanStack Query**: Server state management, caching, background refetching
- **React 18**: Concurrent features, automatic batching, Suspense improvements

**Alternatives Considered**:
- Vue 3: Rejected - team expertise in React
- Angular: Rejected - heavier, less flexibility for this use case
- Plain CSS/Styled Components: Rejected - MUI provides design system consistency

## 4. Database: PostgreSQL 16 with Exclusion Constraints

**Decision**: Use PostgreSQL 16 with GIST exclusion constraints for booking conflicts

**Rationale**:
- **Exclusion Constraints**: Declarative prevention of double-booking at DB level (race-condition proof)
- **tsrange + GIST**: Efficient range overlap detection
- **Advanced Types**: citext (case-insensitive email), uuid, enums
- **Extensions**: pgcrypto (UUIDs), btree_gist (exclusion), citext
- **Maturity**: Proven at scale, strong consistency

**Alternatives Considered**:
- SQL Server: Rejected - licensing costs, Linux support secondary
- MySQL: Rejected - no native exclusion constraints, weaker range types
- Application-level locking: Rejected - distributed race conditions

## 5. Authentication: Microsoft Entra ID (OIDC)

**Decision**: Microsoft Entra ID as sole identity provider via OIDC Authorization Code + PKCE

**Rationale**:
- **Enterprise Standard**: Corporate directory already in place
- **Security**: Conditional Access, MFA, PIM, Identity Protection
- **No Password Storage**: Zero credential liability
- **Rich Claims**: Groups, roles, department, job title for authorization
- **Single Sign-On**: Seamless with other corporate apps

**Token Handling**:
- Access tokens: Short-lived (1hr), validated via JWKS
- Refresh tokens: HttpOnly Secure cookies (backend) / In-memory (frontend)
- PKCE: Mandatory for SPA public client

## 6. Authorization: Hybrid RBAC + ABAC

**Decision**: Role-Based (Application Roles) + Attribute-Based (Business Profiles + Resource Policies)

**Rationale**:
- **Application Roles** (USER, ROOM_ADMIN, SUPPORT, GLOBAL_ADMIN): Administrative boundaries
- **Business Profiles** (COLLABORATOR...PARTNER): Functional permissions by job level
- **Resource Access Policies**: Matrix of Profile × Resource Type → Permissions
- **Exceptions**: Time-bound overrides for specific users

**Why Not Pure RBAC**: Business rules depend on resource type + profile combination (e.g., LEADER can book closed offices, COLLABORATOR cannot) - requires attribute evaluation.

## 7. Reservation Conflict Prevention: Database-Level Exclusion Constraints

**Decision**: Enforce no-overlap at PostgreSQL level using `EXCLUDE USING GIST`

```sql
ALTER TABLE reservations ADD CONSTRAINT ex_no_resource_overlap
EXCLUDE USING gist (
    resource_id WITH =,
    tsrange(reservation_date + start_time, reservation_date + end_time, '[)') WITH &&
) WHERE (status IN ('CONFIRMED', 'CHECKED_IN'));
```

**Rationale**:
- **Race Condition Proof**: Works under concurrent INSERT/UPDATE
- **Declarative**: No application code to maintain
- **Performance**: GIST index optimized for range queries
- **Partial Index**: Only active reservations constrained

**Application Layer**: Still validates early for UX, but DB is source of truth.

## 8. QR Check-in: Public QR ID per Resource

**Decision**: Each office resource has a `public_qr_id` (UUID v4), meeting rooms have NULL

**Rationale**:
- **Security**: QR contains only random UUID, no PII or credentials
- **Rotation**: `qr_version` allows invalidating old QR codes
- **Validation**: Backend resolves QR → Resource → Active Reservation
- **Constraint**: `ck_resource_qr_policy` enforces QR only for offices

**Flow**: User scans → Frontend calls `/check-in/{publicQrId}` → Backend validates reservation → User confirms → Check-in recorded.

## 9. Notifications: Transactional Outbox Pattern

**Decision**: `notification_outbox` table polled by background worker

**Rationale**:
- **Reliability**: Email sent in same transaction as business event (or not at all)
- **Retry Logic**: Exponential backoff, max retries, dead letter tracking
- **Observability**: Full audit of notification lifecycle
- **Scalability**: Worker can be scaled independently

**Alternative Considered**: Direct SMTP in request - rejected (latency, failure coupling).

## 10. Audit Logging: Middleware + Domain Events

**Decision**: Dual approach - HTTP middleware for all mutating requests + explicit domain events for business actions

**Rationale**:
- **Middleware**: Captures all POST/PUT/DELETE with request/response, user, IP, correlation ID
- **Domain Events**: Rich business context (before/after values, reason, entity)
- **Immutable**: Append-only table, never updated/deleted
- **Queryable**: Indexed by actor, entity, action, time

## 11. State Management: TanStack Query + React Context

**Decision**: TanStack Query for server state, React Context for client state (auth, theme)

**Rationale**:
- **TanStack Query**: Caching, deduplication, background updates, stale-while-revalidate
- **React Context**: Low-frequency global state (user, permissions, theme)
- **No Redux/Zustand**: Avoids boilerplate; TanStack Query covers 90% of state needs

## 12. Form Handling: React Hook Form + Zod

**Decision**: React Hook Form for performance, Zod for schema validation

**Rationale**:
- **RHF**: Uncontrolled inputs, minimal re-renders, great DX
- **Zod**: TypeScript-first, inference, shared schemas (frontend/backend potential)
- **Integration**: `@hookform/resolvers/zod` seamless

## 13. API Versioning: URL Path Versioning

**Decision**: `/api/v1/` prefix for all endpoints

**Rationale**:
- Explicit, visible, cacheable
- Easy to route in Nginx
- Supports parallel versions during migration

## 14. Error Handling: Result Pattern + ProblemDetails

**Decision**: Use `Result<T>` (success/failure) in Application layer, map to RFC 7807 ProblemDetails in API

**Rationale**:
- **Result Pattern**: Explicit error handling, no exceptions for control flow
- **ProblemDetails**: Standard HTTP error format, machine-readable
- **Mapping**: Pipeline behavior converts Domain/Application errors → HTTP status codes

## 15. Containerization: Multi-stage Docker Builds

**Decision**: Multi-stage builds for both frontend and backend

**Backend**:
```dockerfile
# Stage 1: Build (SDK)
# Stage 2: Runtime (ASP.NET Runtime) - smaller, no SDK
```

**Frontend**:
```dockerfile
# Stage 1: Build (Node) - Vite build
# Stage 2: Nginx - serve static + SPA fallback
```

**Rationale**: Small production images, build-time dependencies excluded.

## 16. Reverse Proxy: Nginx with Security Headers

**Decision**: Nginx for TLS termination, routing, rate limiting, security headers

**Configuration**:
- TLS 1.2/1.3 only
- HSTS, CSP, X-Frame-Options, Referrer-Policy
- Rate limiting: 100 req/s per IP (burst 200)
- gzip + brotli compression
- Static asset caching (1 year for hashed assets)

## 17. Background Jobs: Hangfire (or Hosted Services)

**Decision**: Hangfire for persistent, retryable background jobs

**Rationale**:
- **Persistence**: Jobs survive restarts (PostgreSQL storage)
- **Dashboard**: Built-in monitoring
- **Retries**: Automatic with configurable policy
- **Scheduling**: Cron expressions for recurring (reminders)

**Alternative**: .NET Hosted Services - rejected for reminder scheduling complexity.

## 18. Database Migrations: EF Core Code-First

**Decision**: EF Core migrations as source of truth, applied at deployment

**Rationale**:
- **Version Control**: Migrations in git, reviewable
- **Idempotent**: Safe to re-run
- **Rollback**: `dotnet ef database update <migration>`
- **Seeding**: Initial data via migration `Up()` or separate seed script

**Note**: FRD provides baseline SQL (Anexo A) - first migration creates schema from that.

## 19. Testing Strategy: Pyramid with Contract Tests

**Decision**: Unit (80%+) → Integration → Contract → E2E → Security → Load

**Tools**:
- **Backend**: xUnit, Moq, Testcontainers (PostgreSQL), WebApplicationFactory
- **Frontend**: Vitest, React Testing Library, Playwright (E2E)
- **Contract**: Pact (consumer-driven) or Specmatic
- **Security**: CodeQL (SAST), Dependabot (SCA), OWASP ZAP (DAST)
- **Load**: k6 scripts for critical paths

## 20. Observability: OpenTelemetry + Serilog

**Decision**: Structured logging (Serilog), metrics (Prometheus), tracing (OpenTelemetry)

**Rationale**:
- **Serilog**: Structured JSON, enrichers (correlation ID, user), multiple sinks
- **Prometheus**: `/metrics` endpoint, custom business metrics
- **OpenTelemetry**: Auto-instrumentation for ASP.NET Core, HttpClient, EF Core
- **Correlation IDs**: Flow through frontend → API → Worker → DB

## 21. Configuration: Options Pattern + Environment Variables

**Decision**: Strongly-typed `IOptions<T>` with JSON + Environment Variable overrides

**Rationale**:
- **Type Safety**: POCO classes with validation attributes
- **Environment Override**: `ConnectionStrings__DefaultConnection` convention
- **Secrets**: Docker Secrets / Azure Key Vault in production
- **Validation**: Startup validation fails fast on bad config

## 22. CORS: Restrictive Policy

**Decision**: CORS policy allows only frontend origin, specific headers/methods

```csharp
policy.WithOrigins("https://app.company.com")
      .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
      .WithHeaders("Authorization", "Content-Type", "X-Correlation-ID")
      .AllowCredentials(); // For HttpOnly cookies
```

## 23. Rate Limiting: Dual Layer

**Decision**: Nginx (network) + ASP.NET Core (application) rate limiting

**Nginx**: IP-based, aggressive (DDoS protection)
**ASP.NET Core**: User-based, policy-aware (authenticated endpoints)

## 24. Health Checks: Three Endpoints

**Decision**: `/health/live` (liveness), `/health/ready` (readiness), `/health` (full)

- **Live**: Process running (k8s liveness probe)
- **Ready**: DB reachable, Entra ID reachable, migrations applied (k8s readiness)
- **Full**: Detailed component status (monitoring dashboard)

## 25. Localization: English + Spanish (es-CO)

**Decision**: Resource files for backend, i18next for frontend, default es-CO

**Rationale**: Colombian law firm context, Spanish primary, English for technical terms.

---

*Each major decision above has a corresponding ADR in `docs/adr/`.*