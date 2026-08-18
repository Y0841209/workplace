# Arquitectura Backend - Workplace Booking Platform

## Visión General

Backend desarrollado en **.NET 8** con **ASP.NET Core** siguiendo **Clean Architecture**. La solución se organiza en 4 capas principales con dependencias que apuntan hacia el interior (Domain).

---

## 1. Módulos (Bounded Contexts)

```
src/
├── WorkplaceBooking.Domain           # Capa de dominio (núcleo)
├── WorkplaceBooking.Application      # Casos de uso, CQRS, DTOs, Validators
├── WorkplaceBooking.Infrastructure   # Implementaciones: EF Core, Email, Auth
└── WorkplaceBooking.Api              # Presentación: Controllers, Middleware, DI
```

### Descripción por Capa

| Capa | Responsabilidad | Dependencias |
|------|-----------------|--------------|
| **Domain** | Entidades, Value Objects, Domain Events, Reglas de negocio puras, Interfaces de repositorio | Ninguna (solo .NET BCL) |
| **Application** | Casos de uso (Commands/Queries), DTOs, Validators, Mapeos, Interfaces de servicios externos | Domain |
| **Infrastructure** | EF Core DbContext, Repositorios, Servicios externos (Email, Entra ID), Hangfire, HealthChecks | Domain, Application |
| **Api** | Controllers, Middleware, Filtros, Swagger, Program.cs (Composition Root) | Application, Infrastructure |

---

## 2. Services (Application Layer)

### Services de Dominio (Domain Services)

| Service | Responsabilidad | Ubicación |
|---------|-----------------|-----------|
| `IReservationPolicyService` | Validar límites de reservas, excepciones, perfiles | Domain/Services |
| `IAvailabilityService` | Consultar disponibilidad, conflictos, franjas horarias | Domain/Services |
| `IUserAuthorizationService` | Verificar permisos por perfil/rol, resource_access_policies | Domain/Services |
| `IQrValidationService` | Validar QR, coincidencia public_qr_id, ventana temporal | Domain/Services |

### Services de Aplicación (Application Services)

| Service | Responsabilidad | Patrón |
|---------|-----------------|--------|
| `ReservationAppService` | Orquestar creación, modificación, cancelación, check-in | CQRS Handlers |
| `ResourceAppService` | CRUD recursos, disponibilidad, importación masiva | CQRS Handlers |
| `UserProfileAppService` | Perfiles, roles, asignaciones, excepciones | CQRS Handlers |
| `NotificationAppService` | Enqueue notificaciones, procesar outbox | Background Job |
| `AuditAppService` | Consultar logs, exportar, filtros | CQRS Queries |

### Services de Infraestructura (Infrastructure Services)

| Service | Implementación | Propósito |
|---------|----------------|-----------|
| `EmailService` | `IEmailService` + MailKit/SMTP | Envío notificaciones |
| `EntraIdTokenService` | `ITokenValidationService` | Validar JWT, JWKS cache |
| `HangfireJobService` | `IBackgroundJobClient` | Jobs recurrentes (recordatorios, limpieza) |
| `CurrentUserService` | `ICurrentUserService` | Claims usuario actual (ICurrentUserService) |

---

## 3. Repositories

### Interfaces (Domain/Repositories)

```csharp
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct);
    Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct);
    Task AddAsync(T entity, CancellationToken ct);
    void Update(T entity);
    void Delete(T entity);
}

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}
```

### Repositorios Específicos

| Repositorio | Entidad | Métodos Adicionales |
|-------------|---------|---------------------|
| `IResourceRepository` | Resource | `GetAvailableAsync(date, start, end, type, floor)`, `GetByPublicQrIdAsync(qrId)` |
| `IReservationRepository` | Reservation | `GetFutureActiveByUserAsync(userId)`, `GetByResourceDateAsync(resourceId, date)`, `CountFutureActiveAsync(userId)` |
| `IUserRepository` | AppUser | `GetByEntraIdAsync(entraId)`, `GetWithProfilesAndRolesAsync(userId)` |
| `IUserProfileRepository` | UserBusinessProfile | `GetActiveByUserAsync(userId)` |
| `IUserRoleRepository` | UserApplicationRole | `GetActiveByUserAsync(userId)` |
| `IResourcePolicyRepository` | ResourceAccessPolicy | `GetByTypeAndProfileAsync(typeCode, profileCode)` |
| `IExceptionRepository` | ReservationException | `GetActiveByUserAsync(userId, resourceType?)` |
| `INotificationRepository` | NotificationOutbox | `GetPendingAsync(batchSize)`, `MarkSentAsync(id)`, `MarkFailedAsync(id, error)` |
| `IAuditRepository` | AuditLog | `QueryAsync(filters, pagination)` |

### Implementación (Infrastructure/Persistence)

- **Base**: `EfRepository<T>` genérico con `DbContext`
- **Especificaciones**: `Ardalis.Specification` para queries complejas
- **UoW**: `AppDbContext` implementa `IUnitOfWork`

---

## 4. CQRS (Command Query Responsibility Segregation)

### Librería: MediatR

### Commands (Escritura)

| Command | Handler | Validación | Retorno |
|---------|---------|------------|---------|
| `CreateReservationCommand` | `CreateReservationHandler` | `CreateReservationValidator` | `Result<ReservationDto>` |
| `UpdateReservationCommand` | `UpdateReservationHandler` | `UpdateReservationValidator` | `Result<ReservationDto>` |
| `CancelReservationCommand` | `CancelReservationHandler` | `CancelReservationValidator` | `Result` |
| `CheckInReservationCommand` | `CheckInReservationHandler` | `CheckInReservationValidator` | `Result<CheckInDto>` |
| `CreateResourceCommand` | `CreateResourceHandler` | `CreateResourceValidator` | `Result<ResourceDto>` |
| `UpdateResourceCommand` | `UpdateResourceHandler` | `UpdateResourceValidator` | `Result<ResourceDto>` |
| `DeleteResourceCommand` | `DeleteResourceHandler` | - | `Result` |
| `AssignUserProfileCommand` | `AssignUserProfileHandler` | `AssignUserProfileValidator` | `Result` |
| `AssignUserRoleCommand` | `AssignUserRoleHandler` | `AssignUserRoleValidator` | `Result` |
| `CreateExceptionCommand` | `CreateExceptionHandler` | `CreateExceptionValidator` | `Result` |

### Queries (Lectura)

| Query | Handler | Retorno |
|-------|---------|---------|
| `GetResourceByIdQuery` | `GetResourceByIdHandler` | `ResourceDto?` |
| `GetResourcesQuery` (paginado, filtros) | `GetResourcesHandler` | `PagedResult<ResourceDto>` |
| `GetAvailabilityQuery` | `GetAvailabilityHandler` | `IReadOnlyList<AvailabilitySlotDto>` |
| `GetMyReservationsQuery` | `GetMyReservationsHandler` | `PagedResult<ReservationDto>` |
| `GetReservationByIdQuery` | `GetReservationByIdHandler` | `ReservationDto?` |
| `GetResourceByQrQuery` | `GetResourceByQrHandler` | `ResourceWithAvailabilityDto?` |
| `GetUserProfileQuery` | `GetUserProfileHandler` | `UserProfileDto?` |
| `GetAuditLogsQuery` | `GetAuditLogsHandler` | `PagedResult<AuditLogDto>` |

### Pipeline Behaviors (MediatR)

| Behavior | Orden | Propósito |
|----------|-------|-----------|
| `ValidationBehavior` | 1 | Ejecuta FluentValidation, lanza `ValidationException` |
| `AuthorizationBehavior` | 2 | Verifica policies/claims via `IAuthorizationService` |
| `LoggingBehavior` | 3 | Log request/response, duración, correlation ID |
| `TransactionBehavior` | 4 | `BeginTransaction` → `SaveChanges` → `Commit/Rollback` |
| `AuditBehavior` | 5 | Publica `DomainEvents` → `AuditLog` via outbox |

---

## 5. Validators (FluentValidation)

### Validadores de Commands

| Validador | Reglas Principales |
|-----------|-------------------|
| `CreateReservationValidator` | `resource_id` existe y reservable, `reservation_date` ≥ today, `start_time` < `end_time`, duración ≥ 60 min, `end_time` ≤ 23:59, `attendee_count` ≤ capacity (salas) |
| `UpdateReservationValidator` | Mismas reglas + reserva existe, usuario owner o SUPPORT, motivo si SUPPORT |
| `CancelReservationValidator` | Reserva existe, usuario owner o SUPPORT, motivo si SUPPORT |
| `CheckInReservationValidator` | `reservation_id` existe, `scanned_qr_id` formato UUID |
| `CreateResourceValidator` | `code` único, `resource_type_code` válido, `capacity` > 0, QR policy según tipo |
| `AssignUserProfileValidator` | `user_id` existe, `profile_code` válido, `valid_from` ≤ `expires_at` |
| `AssignUserRoleValidator` | `user_id` existe, `role_code` válido, no duplicado activo |
| `CreateExceptionValidator` | `user_id` existe, `max_reservations` > 0, `valid_from` ≤ `expires_at`, motivo obligatorio |

### Validadores de Queries

| Validador | Reglas |
|-----------|--------|
| `GetAvailabilityValidator` | `date` ≥ today, `start_time` < `end_time`, `capacity` ≥ 1 si sala |
| `GetResourcesValidator` | `page` ≥ 1, `page_size` 1-100 |

### Registro

```csharp
services.AddValidatorsFromAssemblyContaining<CreateReservationValidator>();
```

---

## 6. Middleware (Api Layer)

### Pipeline Order (Program.cs)

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();     // 1. Global error handling
app.UseMiddleware<CorrelationIdMiddleware>();         // 2. X-Correlation-ID header
app.UseMiddleware<RequestResponseLoggingMiddleware>(); // 3. Log request/response
app.UseMiddleware<SecurityHeadersMiddleware>();       // 4. CSP, HSTS, etc.

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AuditLoggingMiddleware>();          // 5. Audit logging automático
app.MapControllers();
app.UseHealthChecks("/health");
```

### Middleware Personalizados

| Middleware | Propósito | Detalles |
|------------|-----------|----------|
| `ExceptionHandlingMiddleware` | Capturar excepciones no controladas, mapear a ProblemDetails (RFC 7807) | Mapea `ValidationException` → 400, `UnauthorizedAccessException` → 401, `ForbiddenAccessException` → 403, `NotFoundException` → 404, `ConflictException` → 409, resto → 500 |
| `CorrelationIdMiddleware` | Generar/propagar `X-Correlation-ID` (Guid) | Header de entrada o generar nuevo, agregar a response header y `Activity.Current` |
| `RequestResponseLoggingMiddleware` | Log structured request/response | Serializa body (max 10KB), headers, duración, status code, user ID |
| `SecurityHeadersMiddleware` | Headers de seguridad | CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy |
| `AuditLoggingMiddleware` | Auditoría automática mutaciones | Captura actor, acción, entidad, before/after (JSON), IP, UA, correlation ID → `AuditLog` |

---

## 7. Logging (Serilog)

### Configuración (Program.cs)

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "WorkplaceBooking.Api")
    .Enrich.WithProperty("Environment", env.EnvironmentName)
    .Enrich.With<CorrelationIdEnricher>()
    .Enrich.With<SpanIdEnricher>()
    .Enrich.With<TraceIdEnricher>()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        formatter: new JsonFormatter())
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .WriteTo.Seq("http://seq:5341") // Opcional
    .CreateLogger();

builder.Host.UseSerilog();
```

### Enrichers Personalizados

| Enricher | Propiedad Agregada |
|----------|-------------------|
| `CorrelationIdEnricher` | `CorrelationId` (de header o Activity) |
| `SpanIdEnricher` | `SpanId` (OpenTelemetry) |
| `TraceIdEnricher` | `TraceId` (OpenTelemetry) |
| `UserIdEnricher` | `UserId` (claim sub/oid si autenticado) |

### Niveles por Contexto

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "System.Net.Http.HttpClient": "Warning"
    }
  }
}
```

---

## 8. Auditoría

### Estrategia Dual

| Enfoque | Implementación | Cobertura |
|---------|----------------|-----------|
| **Middleware** | `AuditLoggingMiddleware` | Todas las mutaciones HTTP (POST, PUT, PATCH, DELETE) |
| **Domain Events** | `IAuditService` + `DomainEventDispatcher` | Acciones de negocio ricas (before/after, motivo) |

### AuditLoggingMiddleware

```csharp
public async Task InvokeAsync(HttpContext context)
{
    if (!IsMutatingMethod(context.Request.Method)) { await _next(context); return; }

    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-ID"] = correlationId;

    // Capturar request body
    // Capturar response body
    // Construir AuditLogEntry
    // _auditLogger.LogAsync(entry);
}
```

### Domain Events + IAuditService

```csharp
public interface IAuditService
{
    Task LogAsync(AuditLogEntry entry);
    Task LogDomainEventAsync(IDomainEvent domainEvent, IAggregateRoot entity, CancellationToken ct);
}
```

### Entidades de Auditoría

| Evento | Before | After | Motivo |
|--------|--------|-------|--------|
| `ReservationCreated` | null | ReservationDto | - |
| `ReservationModified` | ReservationDto (old) | ReservationDto (new) | `support_change_reason` |
| `ReservationCancelled` | ReservationDto | null | `cancellation_reason` |
| `ReservationCheckedIn` | ReservationDto | ReservationDto (status=CHECKED_IN) | - |
| `UserProfileAssigned` | null | UserBusinessProfileDto | `assignment_reason` |
| `UserRoleAssigned` | null | UserApplicationRoleDto | `assignment_reason` |
| `ExceptionCreated` | null | ReservationExceptionDto | `reason` |
| `ResourceCreated/Modified/Deleted` | ResourceDto | ResourceDto | - |

### Almacenamiento

- Tabla: `audit_logs` (inmutable, append-only)
- Índices: `actor_user_id + created_at DESC`, `entity_name + entity_id`, `action + created_at DESC`, `correlation_id`
- Retención: 7 años (configurable), archivado mensual a cold storage

---

## Diagrama de Dependencias (Clean Architecture)

```
┌─────────────────────────────────────────────────────────────────┐
│                        WORKPLACEBOOKING.API                     │
│  Controllers │ Middleware │ Filters │ Swagger │ Program.cs     │
└────────────────────────────┬────────────────────────────────────┘
                             │ depends on
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                   WORKPLACEBOOKING.APPLICATION                  │
│  Commands/Queries │ Handlers │ Validators │ DTOs │ Interfaces  │
└────────────────────────────┬────────────────────────────────────┘
                             │ depends on
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      WORKPLACEBOOKING.DOMAIN                    │
│  Entities │ ValueObjects │ DomainEvents │ Interfaces │ Rules   │
└─────────────────────────────────────────────────────────────────┘
                             ▲
                             │ implements
┌────────────────────────────┴────────────────────────────────────┐
│                   WORKPLACEBOOKING.INFRASTRUCTURE               │
│  EfRepository │ DbContext │ EmailService │ EntraIdService      │
│  HangfireJobs │ CurrentUserService │ Migrations │ HealthChecks │
└─────────────────────────────────────────────────────────────────┘
```

---

## Configuración de DI (Program.cs - Composition Root)

```csharp
// Domain Services
builder.Services.AddScoped<IReservationPolicyService, ReservationPolicyService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IUserAuthorizationService, UserAuthorizationService>();
builder.Services.AddScoped<IQrValidationService, QrValidationService();

// Application Services (MediatR)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyReference).Assembly));
builder.Services.AddValidatorsFromAssemblyContaining<CreateReservationValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

// Infrastructure
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connString));
builder.Services.AddScoped<IUnitOfWork, AppDbContext>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
// ... demás repositorios

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenValidationService, EntraIdTokenService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Hangfire
builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(connString));
builder.Services.AddHangfireServer();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
```

---

## Health Checks

| Endpoint | Comprobación |
|----------|--------------|
| `/health/live` | Proceso vivo (k8s liveness) |
| `/health/ready` | DB conectable, migraciones aplicadas, Entra ID reachable |
| `/health` | Detallado: DB, Entra ID, Email, Hangfire, Disk space |

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connString, name: "postgres", tags: new[] { "ready" })
    .AddUrlGroup(new Uri("https://login.microsoftonline.com/{tenant}/v2.0/.well-known/openid-configuration"), name: "entra-id", tags: new[] { "ready" })
    .AddCheck<DiskSpaceHealthCheck>("disk-space", tags: new[] { "ready" });
```

---

## Seguridad

| Aspecto | Implementación |
|---------|----------------|
| **Autenticación** | JWT Bearer (Entra ID), validación JWKS, `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime` |
| **Autorización** | Policies: `RequireUser`, `RequireRoomAdmin`, `RequireSupport`, `RequireGlobalAdmin`, `CanReserveResource` (resource-based) |
| **Rate Limiting** | `AspNetCoreRateLimit` por IP + por usuario autenticado |
| **CORS** | Policy estricta: `AllowedOrigins` desde config, `AllowCredentials` |
| **Data Protection** | Keys persistidas en BD / Azure Key Vault |

---

## Observabilidad (OpenTelemetry)

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("WorkplaceBooking.Api")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("WorkplaceBooking.Api")
        .AddPrometheusExporter());
```

### Métricas Custom

| Métrica | Tipo | Descripción |
|---------|------|-------------|
| `booking_reservations_created_total` | Counter | Total reservas creadas |
| `booking_reservations_cancelled_total` | Counter | Total canceladas |
| `booking_checkins_total` | Counter | Total check-ins |
| `booking_availability_search_duration_seconds` | Histogram | Latencia búsqueda disponibilidad |
| `booking_active_reservations` | Gauge | Reservas activas actuales |

---

## Testing Strategy

| Nivel | Herramientas | Cobertura Objetivo |
|-------|--------------|-------------------|
| **Unit** | xUnit, Moq, FluentAssertions | Domain Services, Validators, Handlers (>80%) |
| **Integration** | Testcontainers (PostgreSQL), WebApplicationFactory | Repositories, Handlers con DB real, Middleware |
| **Contract** | Pact / Specmatic | API contracts consumidor/proveedor |
| **Architecture** | NetArchTest / ArchUnitNET | Reglas Clean Architecture, dependencias |

---

## Despliegue (Docker)

```dockerfile
# Multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore && dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "WorkplaceBooking.Api.dll"]
```