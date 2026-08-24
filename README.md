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
│  │  (WorkplaceBooking.Application)                         │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       DOMAIN LAYER                              │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Entities / Value Objects / Domain Events / Interfaces  │   │
│  │  (WorkplaceBooking.Domain)                              │   │
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
│   ├── architecture/          # Architecture documentation
│   └── adr/                   # Architecture Decision Records
├── backend/                   # .NET 8 backend (Clean Architecture)
│   ├── src/
│   │   ├── WorkplaceBooking.Domain/          # Domain layer (pure C#)
│   │   ├── WorkplaceBooking.Application/     # Application layer (CQRS/MediatR)
│   │   ├── WorkplaceBooking.Infrastructure/  # EF Core, repositories, services
│   │   ├── WorkplaceBooking.SharedKernel/    # Primitives, results, exceptions
│   │   └── WorkplaceBooking.API/             # API layer (controllers)
│   ├── tests/                 # xUnit test projects
│   └── WorkplaceBooking.sln
├── frontend/                  # React + TypeScript + MUI (scaffold)
│   ├── src/
│   └── Dockerfile
├── database/
│   └── scripts/               # SQL bootstrap (001 schema -> 009 seed)
├── infrastructure/
│   ├── docker/                # Docker Compose (base + dev override)
│   ├── nginx/                 # Nginx configs (dev + production TLS)
│   └── database/              # Seed helpers
├── .github/workflows/         # CI/CD (GitHub Actions)
└── render.yaml                # Render Blueprint (deploy para pruebas)
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
# Frontend: http://localhost (Nginx reverse proxy)
# API: http://localhost:8080
# Swagger: http://localhost:8080/swagger
# Scalar: http://localhost:8080/scalar
# pgAdmin: http://localhost:5050 (admin@booking.local / admin) [dev override]
# SMTP UI: http://localhost:2500 [dev override]
```

### Local Development (without Docker)

**Backend:**
```bash
cd backend
dotnet restore WorkplaceBooking.sln
# Database: create schema + extensions, then apply scripts in order
#   psql ... -f database/scripts/001_extensions_schema.sql   (extensions + schema)
#   psql ... -f database/scripts/003_users_roles_profiles.sql
#   ... up to 008_seed_data.sql
dotnet run --project src/WorkplaceBooking.Api
```

**Frontend:**
```bash
cd frontend
npm install
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
cd backend
dotnet test WorkplaceBooking.sln --configuration Release --collect:"XPlat Code Coverage"

# Frontend (scaffold; add tests as pages are implemented)
cd frontend
npm run lint          # ESLint
npm run build         # TypeScript check + Vite build
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

## 🚀 Deploy en Render (pruebas)

El repo incluye `render.yaml` (Blueprint) que crea **PostgreSQL + API**:

1. Sube el repo a GitHub y en Render: **New → Blueprint → selecciona el repo**.
2. Llena los secretos marcados `sync: false` (AzureAd / Email) en el dashboard.
3. Bootstrap de la BD (una sola vez): ejecuta `database/bootstrap_full.sql`
   (script combinado que crea extensiones, esquema `booking`, tablas, seed de 91
   recursos y el usuario de desarrollo). Alternativa manual: ejecutar
   `database/scripts/001_...` a `008_seed_data.sql` en orden.
4. Verifica: `GET https://<api>.onrender.com/health/live` → `Healthy`.
5. CI/CD: agrega los secrets `RENDER_DEPLOY_HOOK_URL` y `RENDER_APP_URL` en GitHub para
   auto-desplegar en cada push a `main`.

> Nota: en pruebas la API corre con `ASPNETCORE_ENVIRONMENT=Development` (auth local,
> sin Entra ID). Para producción cambia a `Production` y configura Entra ID real.

## 📄 License

Internal use only - Confidential.

---

*Built with Clean Architecture principles for maintainability, testability, and scalability.*