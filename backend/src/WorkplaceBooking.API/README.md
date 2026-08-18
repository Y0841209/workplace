# Workplace Booking Platform API

REST API for the Workplace Booking Platform built with .NET 8, following Clean Architecture principles.

## Features

- **Authentication**: Microsoft Entra ID (Azure AD) with JWT Bearer tokens
- **Authorization**: Role-based (USER, ROOM_ADMIN, SUPPORT, GLOBAL_ADMIN)
- **Rate Limiting**: Fixed window rate limiting (100 req/s API, 10 req/min auth)
- **Health Checks**: Liveness, readiness, and detailed health endpoints
- **Swagger/OpenAPI**: Interactive API documentation with Scalar UI
- **Rate Limiting**: Built-in rate limiting middleware
- **CORS**: Configurable CORS policy
- **Observability**: Serilog structured logging, OpenTelemetry ready

## Tech Stack

- **.NET 8** / ASP.NET Core 8
- **MediatR** for CQRS
- **FluentValidation** for request validation
- **Entity Framework Core** with PostgreSQL
- **AutoMapper** for object mapping
- **FluentValidation** for request validation
- **Ardalis.Result** for result handling
- **Ardalis.Specification** for query specifications
- **MediatR** for CQRS
- **AutoMapper** for object mapping
- **FluentValidation** for validation
- **Swashbuckle/Scalar** for API documentation
- **Serilog** for structured logging
- **OpenTelemetry** for distributed tracing

## Prerequisites

- .NET 8 SDK
- PostgreSQL 16
- Docker (optional, for containerization)

## Configuration

### Environment Variables / appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=booking;Username=booking_user;Password=your_password"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "00000000-0000-0000-0000-000000000000",
    "ClientId": "00000000-0000-0000-0000-000000000000",
    "ClientSecret": "your_client_secret"
  },
  "Email": {
    "SmtpHost": "smtp.office365.com",
    "SmtpPort": 587,
    "SmtpUser": "noreply@company.com",
    "SmtpPassword": "your_password",
    "FromAddress": "noreply@company.com",
    "FromName": "Workplace Booking Platform"
  },
  "AllowedOrigins": [
    "https://localhost:3000",
    "https://booking.company.com"
  ]
}
```

## Running the Application

### Local Development

```bash
# Prerequisites
# 1. Install .NET 8 SDK
# 2. Start PostgreSQL (or use docker-compose)

# Run database migrations
cd backend
dotnet ef database update --project src/WorkplaceBooking.Infrastructure --startup-project src/WorkplaceBooking.Api

# Run the API
cd src/WorkplaceBooking.Api
dotnet run
```

### With Docker Compose (Development)

```bash
cd src/WorkplaceBooking.Api
docker-compose up -d
```

Services:
- API: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger
- Scalar API Reference: http://localhost:8080/scalar
- pgAdmin: http://localhost:5050 (admin@booking.local / admin)
- SMTP4Dev: http://localhost:2500

### Production Build

```bash
# Build Docker image
docker build -t workplace-booking-api -f src/WorkplaceBooking.Api/Dockerfile .

# Run container
docker run -d \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=db;Database=booking;Username=user;Password=pass" \
  -e AzureAd__TenantId=xxx \
  -e AzureAd__ClientId=xxx \
  -e AzureAd__ClientSecret=xxx \
  workplace-booking-api
```

## API Endpoints

### Resources
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/resources` | List resources (paginated, filterable) |
| GET | `/api/v1/resources/types` | Get resource types |
| GET | `/api/v1/resources/{id}` | Get resource by ID |
| POST | `/api/v1/resources` | Create resource (GLOBAL_ADMIN) |
| PUT | `/api/v1/resources/{id}` | Update resource (GLOBAL_ADMIN) |
| DELETE | `/api/v1/resources/{id}` | Delete resource (GLOBAL_ADMIN) |
| POST | `/api/v1/resources/{id}/regenerate-qr` | Regenerate QR (GLOBAL_ADMIN) |
| POST | `/api/v1/resources/import` | Bulk import (GLOBAL_ADMIN) |
| GET | `/api/v1/resources/availability` | Search available resources |
| GET | `/api/v1/resources/by-floor/{floorId}` | Resources by floor |
| GET | `/api/v1/resources/meeting-rooms` | Get meeting rooms |

### Reservations
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/reservations` | Create reservation |
| GET | `/api/v1/reservations/my` | Get my reservations |
| GET | `/api/v1/reservations/{id}` | Get reservation by ID |
| PUT | `/api/v1/reservations/{id}` | Update reservation |
| POST | `/api/v1/reservations/{id}/cancel` | Cancel reservation |
| POST | `/api/v1/reservations/{id}/check-in` | Check-in |
| POST | `/api/v1/reservations/{id}/check-out` | Check-out |
| GET | `/api/v1/reservations/availability` | Search availability |
| GET | `/api/v1/reservations/check-in/{qrId}` | Get resource for check-in (public) |

### Check-ins
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/check-ins/history` | Get check-in history |
| GET | `/api/v1/check-ins/resource/{id}` | Get resource check-ins (ADMIN) |
| GET | `/api/v1/check-ins/today` | Get today's check-ins |

### Users
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/users/me` | Get current user profile |
| GET | `/api/v1/users/{id}` | Get user by ID (ADMIN/SUPPORT) |
| POST | `/api/v1/users/{id}/profiles` | Assign profile (ADMIN) |
| POST | `/api/v1/users/{id}/roles` | Assign role (ADMIN) |
| POST | `/api/v1/users/{id}/exceptions` | Create exception (ADMIN) |
| GET | `/api/v1/users/me/profiles` | Get my profiles |
| GET | `/api/v1/users/me/roles` | Get my roles |
| GET | `/api/v1/users/me/exceptions` | Get my exceptions |

### Health Checks
| Endpoint | Description |
|----------|-------------|
| `GET /health` | Full health check |
| `GET /health/live` | Liveness probe |
| `GET /health/ready` | Readiness probe |
| `GET /healthchecks-ui` | Health Checks UI |

## API Documentation

- **Swagger UI**: `/swagger`
- **Scalar API Reference**: `/scalar/v1`

## Authentication

The API uses Microsoft Entra ID (Azure AD) with JWT Bearer tokens.

### Token Acquisition (SPA)
1. Redirect to `/auth/login?redirectUrl=/dashboard`
2. User authenticates with Microsoft Entra ID
3. Redirect back with authorization code
4. Exchange code for tokens at `/auth/callback`
5. Include `Authorization: Bearer <access_token>` in requests

### Token Validation
- Validates issuer, audience, lifetime, and signature
- Uses JWKS from Microsoft Entra ID
- Short-lived access tokens (1 hour) with refresh token rotation

## Roles & Permissions

| Role | Permissions |
|------|-------------|
| USER | Create/view/cancel own reservations, check-in/out |
| ROOM_ADMIN | All USER + unlimited meeting room reservations |
| SUPPORT | Modify/cancel any reservation, view audit logs |
| GLOBAL_ADMIN | Full access: resources, users, roles, exceptions, audit |

## Rate Limiting

| Endpoint | Limit |
|----------|-------|
| API (general) | 100 req/s |
| Auth endpoints | 10 req/min |

## Development

### Project Structure
```
backend/
├── src/
│   ├── WorkplaceBooking.Api/          # API Controllers, Middleware
│   ├── WorkplaceBooking.Application/  # CQRS Handlers, DTOs, Validators
│   ├── WorkplaceBooking.Domain/       # Entities, Domain Services, Events
│   ├── WorkplaceBooking.Infrastructure/ # EF Core, Repositories, Services
│   └── WorkplaceBooking.SharedKernel/ # Primitives, Results, Exceptions
├── tests/
│   ├── WorkplaceBooking.Api.Tests/
│   ├── WorkplaceBooking.Application.Tests/
│   ├── WorkplaceBooking.Domain.Tests/
│   └── WorkplaceBooking.Infrastructure.Tests/
├── infrastructure/
│   ├── docker/
│   └── nginx/
└── database/
    └── scripts/
```

### Running Tests
```bash
# Unit tests
dotnet test backend/tests/

# With coverage
dotnet test backend/tests/ --collect:"XPlat Code Coverage"
```

### Database Migrations
```bash
# Add migration
dotnet ef migrations add MigrationName \
  --project src/WorkplaceBooking.Infrastructure \
  --startup-project src/WorkplaceBooking.Api

# Update database
dotnet ef database update \
  --project src/WorkplaceBooking.Infrastructure \
  --startup-project src/WorkplaceBooking.Api
```

## Deployment

### Production Checklist
- [ ] Configure Azure AD App Registration
- [ ] Set up PostgreSQL with SSL
- [ ] Configure SMTP for emails
- [ ] Set up TLS certificates (Let's Encrypt)
- [ ] Configure Nginx reverse proxy
- [ ] Set up monitoring (Prometheus/Grafana)
- [ ] Configure backup strategy
- [ ] Set up CI/CD pipeline

### Docker Production
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## License

Internal use only - Confidential.