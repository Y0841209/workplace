# Workplace Booking Platform - Architecture Documentation

## Overview

This document describes the complete system architecture for the Workplace Booking Platform, a responsive web application for booking open workspaces, closed offices, and meeting rooms. Built following Clean Architecture principles with a modern tech stack.

## Technology Stack

| Layer | Technology | Version |
|-------|------------|---------|
| Frontend | React + TypeScript + Material UI | React 18, TypeScript 5, MUI 5 |
| Backend | .NET 8 Web API | .NET 8.0 LTS |
| Database | PostgreSQL | 16 |
| Authentication | Microsoft Entra ID | OIDC/OAuth 2.0 |
| Infrastructure | Ubuntu Server + Docker Compose + Nginx | 24.04 LTS |
| CI/CD | GitHub Actions | Latest |
| Reporting | Power BI | Connected to PostgreSQL |

## Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                       │
│  ┌─────────────────────┐    ┌─────────────────────────────┐   │
│  │   React Frontend    │    │   .NET 8 Web API (Controllers)  │
│  │  (Material UI)      │    │   (API Endpoints)             │
│  └─────────────────────┘    └─────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Use Cases / Application Services / DTOs / Validators   │   │
│  │  (BookingPlatform.Application)                          │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       DOMAIN LAYER                              │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Entities / Value Objects / Domain Events / Interfaces  │   │
│  │  (BookingPlatform.Domain)                               │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   INFRASTRUCTURE LAYER                          │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────┐ ┌───────────┐ │
│  │ EF Core      │ │ Identity     │ │ Email    │ │ Background│ │
│  │ Repositories │ │ (Entra ID)   │ │ Service  │ │ Workers   │ │
│  │              │ │              │ │          │ │           │ │
│  │ (BookingPlat-│ │              │ │          │ │           │ │
│  │ form.Infra-  │ │              │ │          │ │           │ │
│  │ structure)   │ │              │ │          │ │           │ │
│  └──────────────┘ └──────────────┘ └──────────┘ └───────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        DATA LAYER                               │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              PostgreSQL 16 (booking schema)             │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## Domain Model

### Core Entities

1. **Location** - Physical office locations (multi-tenant ready)
2. **Floor** - Floors within a location
3. **Zone** - Logical groupings within floors
4. **ResourceType** - OPEN_WORKSPACE, CLOSED_OFFICE, MEETING_ROOM
5. **Resource** - Bookable spaces (offices, meeting rooms)
6. **AppUser** - Users synchronized with Microsoft Entra ID
7. **BusinessProfile** - COLLABORATOR, ASSOCIATE, LEADER, DIRECTOR, PARTNER
8. **ApplicationRole** - USER, ROOM_ADMIN, SUPPORT, GLOBAL_ADMIN
9. **Reservation** - Time-based bookings with status tracking
10. **CheckIn** - QR-based check-in for offices
11. **NotificationOutbox** - Email notification queue
12. **AuditLog** - Security and business audit trail

### Key Business Rules (Enforced at DB + Application Level)

- Minimum reservation duration: 1 hour
- Same-day reservations only (no cross-day)
- Maximum end time: 23:59
- Maximum 5 future active reservations per user
- ROOM_ADMIN exception: unlimited future reservations for MEETING_ROOM only
- QR/Check-in only for OPEN_WORKSPACE and CLOSED_OFFICE
- Meeting rooms: no QR, no check-in, capacity validation
- Exclusion constraints prevent double-booking (resource + user)

## API Design

### REST Endpoints (v1)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/resources` | List resources (filterable) |
| GET | `/api/v1/resources/{id}/availability` | Resource availability |
| GET | `/api/v1/availability` | Search available resources |
| POST | `/api/v1/reservations` | Create reservation |
| PUT | `/api/v1/reservations/{id}` | Modify reservation |
| POST | `/api/v1/reservations/{id}/cancel` | Cancel reservation |
| GET | `/api/v1/reservations/mine` | User's reservations |
| GET | `/api/v1/check-in/resources/{publicQrId}` | Resolve QR |
| POST | `/api/v1/reservations/{id}/check-in` | Confirm check-in |
| GET | `/api/v1/admin/audit-logs` | Audit logs (admin) |
| POST | `/api/v1/admin/resources/import` | Bulk resource import |

## Authentication & Authorization

### Microsoft Entra ID Integration

- **Protocol**: OpenID Connect (OIDC) + OAuth 2.0
- **Flow**: Authorization Code with PKCE (SPA)
- **Token Storage**: HttpOnly Secure cookies (backend) / Memory (frontend)
- **Claims Mapping**:
  - `sub` → `entra_object_id`
  - `preferred_username` / `email` → `email`
  - `name` → `display_name`
  - `jobTitle` → `job_title`
  - `department` → `department`

### Authorization Model

```
GLOBAL_ADMIN
├── Full system access
├── User/Role management
├── Exception management
├── Audit log access
└── All resource types

ROOM_ADMIN
├── MEETING_ROOM: Unlimited reservations
├── MEETING_ROOM: Full CRUD
├── Other resources: Standard limits
└── Audit log read

SUPPORT
├── Modify any reservation (with reason)
├── View audit logs
└── Standard user permissions

USER (default)
├── Own reservations CRUD
├── QR check-in (offices)
├── Resource availability search
└── Profile-based resource access
```

### Resource Access Policies (Business Profile + Resource Type)

| Profile | OPEN_WORKSPACE | CLOSED_OFFICE | MEETING_ROOM |
|---------|----------------|---------------|--------------|
| COLLABORATOR | ✓ Reserve | ✗ Reserve | ✓ Reserve |
| ASSOCIATE | ✓ Reserve | ✗ Reserve | ✓ Reserve |
| LEADER | ✓ Reserve | ✓ Reserve | ✓ Reserve |
| DIRECTOR | ✓ Reserve | ✓ Reserve | ✓ Reserve |
| PARTNER | ✓ Reserve | ✓ Reserve | ✓ Reserve |

## Frontend Architecture

### Project Structure (Feature-based)

```
src/
├── components/           # Shared UI components
│   ├── common/          # Buttons, Inputs, Cards, etc.
│   ├── layout/          # Header, Footer, Sidebar, Drawer
│   ├── reservation/     # Reservation-specific components
│   ├── resource/        # Resource cards, lists, filters
│   └── admin/           # Admin-only components
├── pages/               # Route-level components
│   ├── public/          # Login, QR check-in
│   ├── user/            # Dashboard, My Reservations, Book
│   └── admin/           # Admin panels
├── hooks/               # Custom React hooks
├── services/            # API clients, auth service
├── contexts/            # React contexts (Auth, Theme, Notifications)
├── types/               # TypeScript interfaces
├── utils/               # Helpers, formatters, validators
├── theme/               # MUI theme configuration
├── layouts/             # Page layouts
└── assets/              # Static assets
```

### State Management

- **Server State**: TanStack Query (React Query) for API data
- **Client State**: React Context + useReducer for auth, theme, UI state
- **Forms**: React Hook Form + Zod validation
- **Routing**: React Router v6 with protected routes

### Responsive Design

- **Breakpoints**: xs(0), sm(600), md(900), lg(1200), xl(1536)
- **Approach**: Mobile-first with Material UI Grid v2
- **Color Palette**: 
  - Primary: #FFD800 (Yellow)
  - Primary Dark: #0E0E0E (Near Black)
  - Background: #FFFFFF, #F5F5F5, #F6F0CB
  - Text: #0E0E0E, #2A2A2A
- **Accessibility**: WCAG AA minimum

## Backend Architecture

### Project Structure (Clean Architecture)

```
BookingPlatform.Domain/
├── Entities/           # Core domain entities
├── ValueObjects/       # Immutable value objects
├── Enums/              # Domain enums
├── Events/             # Domain events
├── Interfaces/         # Repository interfaces, domain services
├── Exceptions/         # Domain exceptions
└── Specifications/     # Business rule specifications

BookingPlatform.Application/
├── UseCases/           # Application use cases (CQRS pattern)
│   ├── Commands/       # Write operations
│   └── Queries/        # Read operations
├── DTOs/               # Data transfer objects
├── Validators/         # FluentValidation validators
├── Interfaces/         # Application service interfaces
├── Mappings/           # AutoMapper profiles
├── Behaviors/          # MediatR pipeline behaviors
└── Common/             # Shared application logic

BookingPlatform.Infrastructure/
├── Persistence/        # EF Core DbContext, Repositories
│   ├── Configurations/ # Entity configurations
│   ├── Migrations/     # EF Core migrations
│   └── Repositories/   # Repository implementations
├── Identity/           # Entra ID integration
├── Email/              # Email service implementation
├── BackgroundJobs/     # Hangfire/Quartz workers
├── Security/           # Encryption, hashing
└── Common/             # Shared infrastructure

BookingPlatform.Api/
├── Controllers/        # API Controllers
├── Middleware/         # Custom middleware
├── Filters/            # Action filters
├── Extensions/         # Service collection extensions
├── Configuration/      # App settings binding
└── Program.cs          # Entry point
```

### Key Patterns

- **CQRS**: Commands (write) / Queries (read) separation via MediatR
- **Repository Pattern**: Abstract data access behind interfaces
- **Unit of Work**: EF Core DbContext as UoW
- **Domain Events**: For cross-aggregate consistency
- **Pipeline Behaviors**: Validation, logging, transaction management
- **Result Pattern**: Explicit success/failure handling (no exceptions for control flow)

## Database Architecture

### Schema: `booking`

```
Extensions: pgcrypto, btree_gist, citext

Core Tables:
├── app_settings (singleton)
├── locations
├── floors
├── zones
├── resource_types (lookup)
├── resources
├── app_users
├── business_profiles (lookup)
├── application_roles (lookup)
├── user_business_profiles
├── user_application_roles
├── resource_access_policies
├── reservation_exceptions
├── reservations
├── checkins
├── notification_outbox
└── audit_logs
```

### Critical Constraints

- **Exclusion Constraints** (GIST + tsrange):
  - No overlapping reservations per resource
  - No overlapping reservations per user
- **Check Constraints**:
  - Minimum 1 hour duration
  - End time ≤ 23:59
  - QR policy: only offices have public_qr_id
- **Triggers**:
  - `updated_at` auto-update
  - Reservation business rules validation
  - Check-in business rules validation

### Indexing Strategy

- Composite indexes for query patterns
- Partial indexes for active/future reservations
- GIST indexes for range exclusion constraints

## Infrastructure Architecture

### Docker Compose Services

```yaml
services:
  postgres:       # PostgreSQL 16
  api:            # .NET 8 Web API
  frontend:       # React + Nginx (build) / Vite (dev)
  nginx:          # Reverse proxy + SSL termination
  hangfire:       # Background job processor (optional)
```

### Nginx Configuration

- Reverse proxy to API and Frontend
- SSL termination (Let's Encrypt / self-signed dev)
- Rate limiting
- Security headers (CSP, HSTS, etc.)
- Gzip compression
- Static asset caching

### Deployment Topology (Ubuntu 24.04 VM)

```
Internet
    │
    ▼
┌─────────────┐
│   Nginx     │  :80/:443
│  (Proxy)    │
└─────────────┘
    │
    ├──────────────────┐
    ▼                  ▼
┌─────────────┐  ┌─────────────┐
│  Frontend   │  │    API      │
│  (Static)   │  │  (.NET 8)   │
└─────────────┘  └─────────────┘
                    │
                    ▼
             ┌─────────────┐
             │ PostgreSQL  │
             │     16      │
             └─────────────┘
```

## Background Processing

### Notification Worker

- Processes `notification_outbox` table
- Sends emails via SMTP (SendGrid / Office 365)
- Retry logic with exponential backoff
- Scheduled: Every minute for pending, 15min before for reminders

### Audit Logging

- Middleware captures all mutating requests
- Domain events for business-critical actions
- Structured JSON logging (Serilog → Seq / Loki)

## Security Considerations

### Data Protection

- HTTPS enforced everywhere
- Secrets in Docker secrets / Azure Key Vault (prod)
- Connection strings encrypted
- PII minimization (no passwords stored)

### API Security

- JWT validation (Entra ID issuer + audience)
- Role/claim-based authorization policies
- Rate limiting per IP/user
- Input validation (FluentValidation)
- CORS restricted to frontend origin

### Database Security

- Parameterized queries (EF Core)
- Row-level security ready (multi-tenant)
- Audit triggers on sensitive tables
- Least privilege DB users

## Observability

### Logging

- Structured logging (Serilog)
- Correlation IDs across requests
- Log levels: Debug, Information, Warning, Error, Fatal
- Output: Console (dev), Seq/ELK (prod)

### Metrics

- Prometheus metrics endpoint
- Custom business metrics (reservations, conflicts, check-ins)
- Health checks: `/health`, `/health/ready`, `/health/live`

### Tracing

- OpenTelemetry integration
- Distributed tracing across frontend → API → DB

## CI/CD Pipeline

### GitHub Actions Workflow

```yaml
stages:
  1. Build & Test
     - Frontend: lint, typecheck, unit, e2e
     - Backend: build, unit, integration, contract tests
  2. Security
     - SAST: CodeQL / SonarQube
     - SCA: Dependabot, Trivy
     - DAST: OWASP ZAP (staging)
  3. Docker Build
     - Multi-stage builds
     - Image scanning
  4. Deploy
     - Staging: auto on main
     - Production: manual approval
```

## Testing Strategy

| Level | Tools | Coverage Target |
|-------|-------|-----------------|
| Unit | xUnit, Moq, Vitest, React Testing Library | 80%+ |
| Integration | Testcontainers (PostgreSQL), WebApplicationFactory | Key flows |
| Contract | Pact / Specmatic | API contracts |
| E2E | Playwright | Critical user journeys |
| Security | OWASP ZAP, CodeQL | All deployments |
| Load | k6 / NBomber | Peak concurrency |

## Performance Requirements

| Metric | Target |
|--------|--------|
| API Response (p95) | < 200ms |
| Availability Search | < 300ms |
| QR Check-in | < 500ms |
| Concurrent Users | 500+ |
| Reservation Throughput | 100/sec |
| DB Connection Pool | 100 (API) / 20 (Workers) |

## Disaster Recovery

- **RPO**: 5 minutes (Point-in-time recovery)
- **RTO**: 30 minutes
- **Backups**: Daily full, hourly WAL archiving
- **Multi-AZ**: Ready for PostgreSQL replication

## Future Extensibility

- Multi-location support (schema ready)
- Cross-day reservations (flag in app_settings)
- Recurring reservations
- Resource amenities/equipment
- Mobile app (React Native / PWA)
- Calendar integration (Outlook/Google)
- Advanced analytics (Power BI datasets)