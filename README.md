# Workplace Booking Platform

> Responsive web application for booking open workspaces, closed offices, and meeting rooms. Built with Clean Architecture, React + TypeScript + Material UI, .NET 8, PostgreSQL 16, and Microsoft Entra ID.

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                       │
│  ┌─────────────────────┐    ┌─────────────────────────────┐   │
│  │   React Frontend    │    │   .NET 8 Web API            │   │
│  │  (Material UI)      │    │   (API Endpoints)           │   │
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

## 🛠️ Technology Stack

| Layer | Technology | Version |
|-------|------------|---------|
| **Frontend** | React + TypeScript + Material UI | React 18, TS 5, MUI 5 |
| **Backend** | .NET 8 Web API | .NET 8.0 LTS |
| **Database** | PostgreSQL | 16 |
| **Auth** | Microsoft Entra ID | OIDC/OAuth 2.0 |
| **Infrastructure** | Ubuntu + Docker Compose + Nginx | 24.04 LTS |
| **CI/CD** | GitHub Actions | Latest |

## 📁 Project Structure

```
workplace-booking-platform/
├── docs/
│   ├── architecture/
│   │   ├── ARCHITECTURE.md      # Complete architecture documentation
│   │   ├── DIAGRAMS.md          # Mermaid diagrams (C4, sequences, ER)
│   │   ├── TECHNICAL_DECISIONS.md
│   │   └── DEPENDENCIES.md
│   └── adr/                     # Architecture Decision Records (18 ADRs)
├── src/
│   ├── backend/
│   │   ├── src/
│   │   │   ├── BookingPlatform.Domain/       # Domain layer (pure C#)
│   │   │   ├── BookingPlatform.Application/  # Application layer (CQRS)
│   │   │   ├── BookingPlatform.Infrastructure/ # Infrastructure (EF Core, Entra ID)
│   │   │   └── BookingPlatform.Api/          # API layer (Controllers)
│   │   └── tests/                           # Unit, Integration, API tests
│   ├── frontend/
│   │   ├── src/
│   │   │   ├── components/      # Reusable UI components
│   │   │   ├── pages/           # Route-level components
│   │   │   ├── hooks/           # Custom React hooks
│   │   │   ├── services/        # API clients, auth
│   │   │   ├── contexts/        # React contexts (Auth, Theme)
│   │   │   ├── types/           # TypeScript interfaces
│   │   │   ├── utils/           # Helpers, formatters
│   │   │   ├── theme/           # MUI theme configuration
│   │   │   ├── layouts/         # Page layouts
│   │   │   └── assets/          # Static assets
│   │   └── tests/               # Unit, E2E tests
│   └── ...
├── infrastructure/
│   ├── docker/                  # Docker Compose files
│   ├── nginx/                   # Nginx configurations
│   └── database/
│       └── migrations/          # SQL migrations / EF Core
└── ...
```

## 🚀 Quick Start

### Prerequisites

- Docker Desktop 4.0+
- .NET 8 SDK (for local backend development)
- Node.js 20 LTS (for local frontend development)
- Git

### Development with Docker Compose

```bash
# Clone and navigate
cd workplace-booking-platform

# Start all services (PostgreSQL, API, Frontend, Nginx, SMTP dev)
docker-compose -f infrastructure/docker/docker-compose.yml \
               -f infrastructure/docker/docker-compose.override.yml up -d

# Access applications
# Frontend: http://localhost:3000 (Vite dev server with HMR)
# API: http://localhost:8080
# Swagger: http://localhost:8080/scalar
# Hangfire: http://localhost:8080/hangfire
# pgAdmin: http://localhost:5050 (admin@booking.local / admin)
# SMTP UI: http://localhost:2500
```

### Local Development (without Docker)

**Backend:**
```bash
cd src/backend
dotnet restore
dotnet ef database update --project src/BookingPlatform.Infrastructure --startup-project src/BookingPlatform.Api
dotnet run --project src/BookingPlatform.Api
```

**Frontend:**
```bash
cd src/frontend
npm ci
npm run dev
```

## 🔐 Authentication

- **Provider**: Microsoft Entra ID (Azure AD)
- **Flow**: Authorization Code + PKCE (SPA)
- **Tokens**: JWT Access Tokens + Refresh Tokens (HttpOnly cookies)
- **Claims**: email, name, jobTitle, department, groups → roles

## 📊 Database Schema

The database uses schema `booking` with key tables:

- `resources` - 91 bookable spaces (60 open, 24 closed, 7 meeting rooms)
- `reservations` - Time-based bookings with exclusion constraints
- `checkins` - QR-based check-ins for offices
- `notification_outbox` - Transactional outbox for emails
- `audit_logs` - Immutable audit trail
- `app_users`, `business_profiles`, `application_roles` - RBAC/ABAC

See [FRD Document](docs/FRD_Modelo_Datos_Workplace_Booking_OpenCode.docx) for complete schema.

## 🎨 Design System

- **Primary**: `#FFD800` (Yellow)
- **Primary Dark**: `#0E0E0E` (Near Black)
- **Backgrounds**: `#FFFFFF`, `#F5F5F5`, `#F6F0CB`
- **Text**: `#0E0E0E`, `#2A2A2A`
- **Breakpoints**: xs(0), sm(600), md(900), lg(1200), xl(1536)
- **Accessibility**: WCAG AA minimum

## 🧪 Testing

```bash
# Backend tests
cd src/backend
dotnet test --configuration Release --collect:"XPlat Code Coverage"

# Frontend tests
cd src/frontend
npm run test:unit          # Unit tests (Vitest + RTL)
npm run test:coverage      # With coverage
npm run test:e2e           # E2E tests (Playwright)
npm run lint               # ESLint
npm run typecheck          # TypeScript check
```

## 📦 CI/CD Pipeline

GitHub Actions workflow includes:

1. **Build & Test** - Backend + Frontend
2. **Security** - CodeQL (SAST), Dependabot (SCA), OWASP ZAP (DAST)
3. **Docker Build** - Multi-stage, multi-arch images
4. **Deploy** - Staging (auto), Production (manual approval)

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/architecture/ARCHITECTURE.md) | Complete system architecture |
| [Diagrams](docs/architecture/DIAGRAMS.md) | Mermaid diagrams (C4, sequences, ER) |
| [Technical Decisions](docs/architecture/TECHNICAL_DECISIONS.md) | 25 key decisions with rationale |
| [Dependencies](docs/architecture/DEPENDENCIES.md) | Full dependency catalog |
| [ADRs](docs/adr/) | 18 Architecture Decision Records |

## 🔒 Security

- OWASP Top 10 mitigated
- HTTPS everywhere (Nginx TLS termination)
- JWT validation with JWKS
- Rate limiting (Nginx + ASP.NET Core)
- CSP, HSTS, security headers
- No secrets in repo (Docker secrets / Key Vault)
- Audit logging on all mutations

## 📈 Observability

- **Logging**: Serilog → Console/Seq (structured JSON)
- **Metrics**: Prometheus `/metrics` endpoint
- **Tracing**: OpenTelemetry → Tempo/Jaeger
- **Health**: `/health`, `/health/live`, `/health/ready`

## 📄 License

Internal use only - Confidential.

---

*Built with Clean Architecture principles for maintainability, testability, and scalability.*