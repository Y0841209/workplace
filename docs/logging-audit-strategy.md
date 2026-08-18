# Workplace Booking Platform - Logging & Audit Strategy

## 1. Serilog

### Configuración Base
- **Sink principal**: Console (JSON estructurado) + File (rolling daily)
- **Formato**: JSON compacto con campos estructurados
- **Enriquecedores obligatorios**:
  - `FromLogContext` (propiedades contextuales)
  - `CorrelationIdEnricher` (TraceId/SpanId de W3C Trace Context)
  - `SpanIdEnricher`, `TraceIdEnricher` (OpenTelemetry)
  - `EnvironmentName`, `ApplicationName`, `MachineName`
  - `ThreadId`, `ProcessId`

### Sinks por Entorno
| Entorno | Sinks Activos | Formato |
|---------|---------------|---------|
| **Development** | Console (human-readable), Seq (opcional) | Pretty JSON / Console colors |
| **Staging/Production** | Console (JSON), File (rolling), Seq/Elasticsearch | Compact JSON |

### Configuración de Niveles
| Namespace | Development | Production |
|-----------|-------------|------------|
| `Default` | Debug | Information |
| `Microsoft` | Information | Warning |
| `Microsoft.AspNetCore` | Information | Warning |
| `Microsoft.EntityFrameworkCore` | Information | Warning |
| `WorkplaceBooking` | Debug | Information |
| `Hangfire` | Information | Information |

### Sinks de Salida
| Sink | Propósito | Retención |
|------|-----------|-----------|
| **Console** | Desarrollo, contenedores | - |
| **File (rolling daily)** | Persistencia local, debugging | 30 días |
| **Seq / Elasticsearch** | Búsqueda, análisis, alertas | 90 días |
| **File (Audit)** | Solo auditoría | 1 año |

---

## 2. Structured Logging

### Principios
- **Solo JSON**: Todos los logs en formato JSON compacto
- **Campos obligatorios** en cada entrada:
  - `Timestamp` (ISO 8601 UTC)
  - `Level` (Debug/Information/Warning/Error/Critical)
  - `MessageTemplate` (template original, no interpolado)
  - `Message` (mensaje renderizado)
  - `TraceId` / `SpanId` (W3C Trace Context)
  - `SourceContext` (namespace/clase)
  - `UserId` / `EntraObjectId` (cuando autenticado)
  - `Action` / `EntityType` / `EntityId` (contexto negocio)
  - `CorrelationId` (X-Correlation-ID header)

### Propiedades de Contexto Enriquecido
| Propiedad | Origen | Ejemplo |
|-----------|--------|---------|
| `TraceId` | `Activity.Current.TraceId` | `00-abc123...` |
| `SpanId` | `Activity.Current.SpanId` | `def456...` |
| `CorrelationId` | Header `X-Correlation-ID` | `req-789...` |
| `UserId` | `ICurrentUserService.UserId` | `guid` |
| `EntraObjectId` | Claim `oid` / `sub` | `guid` |
| `UserEmail` | Claim `email` | `user@company.com` |
| `UserRoles` | Claim `roles` | `["USER","ROOM_ADMIN"]` |
| `BusinessProfiles` | Claim `business_profiles` | `["LEADER"]` |
| `IpAddress` | `HttpContext.Connection.RemoteIpAddress` | `192.168.1.1` |
| `UserAgent` | Header `User-Agent` | `Mozilla/5.0...` |
| `RequestPath` | `HttpContext.Request.Path` | `/api/v1/reservations` |
| `HttpMethod` | `GET/POST/PUT/DELETE` | `POST` |
| `StatusCode` | `200/400/401/403/404/500` | `201` |
| `DurationMs` | `Stopwatch.ElapsedMilliseconds` | `45` |

### Log Context en Código (Patrón)
```csharp
using (LogContext.PushProperty("UserId", userId))
using (LogContext.PushProperty("ResourceId", resourceId))
using (LogContext.PushProperty("Action", "CreateReservation"))
{
    Log.Information("Creating reservation for user {UserId} on resource {ResourceId}", userId, resourceId);
}
```

---

## 3. Correlation ID

### Estrategia
- **Origen**: Header `X-Correlation-ID` (entrada) o generado en API Gateway/Nginx
- **Propagación**: Header `X-Correlation-ID` en todas las llamadas salientes
- **Formato**: UUID v4 o W3C `traceparent` compatible
- **Propagación automática**: Middleware en pipeline ASP.NET Core + HttpClient interceptors

### Flujo de Propagación
```
Client → Nginx (generar/leer X-Correlation-ID)
  → Frontend (React) → Header X-Correlation-ID
  → API Gateway (Nginx) → Header X-Correlation-ID
  → ASP.NET Core Middleware → LogContext.PushProperty("CorrelationId")
  → HttpClient (Saliente) → Header X-Correlation-ID
  → PostgreSQL (ApplicationName + Comment)
  → Hangfire (Job metadata)
  → SMTP (Header X-Correlation-ID)
```

### Headers Estándar
| Header | Dirección | Formato |
|---------|-----------|---------|
| `X-Correlation-ID` | Entrada/Salida | UUID v4 |
| `X-Request-ID` | Nginx → API | UUID (Nginx `$request_id`) |
| `traceparent` | W3C Trace Context | `00-traceId-spanId-flags` |
| `tracestate` | Estado distribuido | `vendor=value` |

---

## 4. Auditoría Funcional

### Eventos Auditable (Funcionales)
| Evento | Entidad | Acción | Detalle |
|--------|---------|--------|---------|
| `ReservationCreated` | Reservation | CREATE | ResourceId, UserId, Date, Start/End |
| `ReservationModified` | Reservation | UPDATE | Campos cambiados, before/after |
| `ReservationCancelled` | Reservation | DELETE | Reason, CancelledBy, IsSupport |
| `ReservationCheckedIn` | Reservation | CHECK_IN | QrId, Window |
| `ReservationCheckedOut` | Reservation | CHECK_OUT | - |
| `ReservationAutoCompleted` | Reservation | AUTO_COMPLETE | Status transition |
| `ResourceCreated` | Resource | CREATE | Type, Floor, Capacity, QR |
| `ResourceModified` | Resource | UPDATE | Cambios, QR rotation |
| `ResourceDeleted` | Resource | DELETE | - |
| `UserProfileAssigned` | UserBusinessProfile | ASSIGN | ProfileCode, ValidFrom/To |
| `UserRoleAssigned` | UserApplicationRole | ASSIGN | RoleCode, ValidFrom/To |
| `ExceptionCreated` | ReservationException | CREATE | Limit, AppliesTo, ValidFrom/To |
| `ExceptionExpired` | ReservationException | EXPIRE | - |
| `CheckInCompleted` | CheckIn | CHECK_IN | QrId, IpAddress, Window |
| `CheckOutCompleted` | CheckIn | CHECK_OUT | - |
| `NotificationSent` | NotificationOutbox | SEND | Type, Recipient, Status |
| `NotificationFailed` | NotificationOutbox | FAILED | Error, RetryCount |

### Campos de Auditoría Funcional
| Campo | Tipo | Obligatorio |
|-------|------|-------------|
| `AuditId` | Guid | Sí |
| `Timestamp` | DateTimeOffset (UTC) | Sí |
| `CorrelationId` | Guid | Sí |
| `ActorUserId` | Guid? | Sí (null = sistema) |
| `ActorEmail` | String | Sí |
| `ActorRoles` | String[] | Sí |
| `Action` | String (enum) | Sí |
| `EntityName` | String | Sí |
| `EntityId` | Guid | Sí |
| `BeforeState` | JSON (nullable) | No |
| `AfterState` | JSON (nullable) | No |
| `Reason` | String (nullable) | No |
| `IpAddress` | String | Sí |
| `UserAgent` | String | Sí |

---

## 5. Auditoría de Seguridad

### Eventos de Seguridad Auditable
| Evento | Severidad | Descripción |
|--------|-----------|-------------|
| `AuthLoginSuccess` | Information | Login exitoso Entra ID |
| `AuthLoginFailed` | Warning | Credenciales inválidas, MFA fallido, cuenta bloqueada |
| `AuthTokenRefreshSuccess` | Information | Refresh token renovado |
| `AuthTokenRefreshFailed` | Warning | Refresh token expirado/revocado |
| `AuthLogout` | Information | Logout explícito |
| `AccessDenied` | Warning | 403 Forbidden - recurso no autorizado |
| `UnauthorizedAccess` | Warning | 401 Unauthorized - token inválido/expirado |
| `PrivilegeEscalationAttempt` | Critical | Intento acceso rol superior |
| `SqlInjectionAttempt` | Critical | Detección patrones SQLi |
| `XssAttempt` | Critical | Detección XSS en input |
| `PathTraversalAttempt` | Critical | Path traversal detectado |
| `RateLimitExceeded` | Warning | Rate limit superado |
| `SuspiciousActivity` | Warning | Patrones anómalos (muchos 404, escaneo) |
| `DataExport` | Information | Exportación datos masiva |
| `BulkOperation` | Information | Operación masiva (import, delete masivo) |
| `ConfigurationChanged` | Warning | Cambio configuración crítica |
| `PermissionChanged` | Warning | Cambio roles/perfiles/excepciones |

### Campos Adicionales Seguridad
| Campo | Descripción |
|-------|-------------|
| `ThreatLevel` | Info/Warning/Critical |
| `AttackVector` | Network/Application/User |
| `SourceIp` | IP origen |
| `GeoLocation` | País/Ciudad (si disponible) |
| `AttackPattern` | SQLi/XSS/PathTraversal/BruteForce |
| `Blocked` | Boolean (WAF/firewall bloqueó) |
| `RuleId` | ID regla WAF/IDS |

---

## 6. Retención de Logs

### Políticas por Tipo de Log

| Tipo de Log | Retención | Almacenamiento | Compresión |
|-------------|-----------|----------------|------------|
| **Application Logs** (Debug/Info) | 7 días | Local SSD / Cloud | gzip |
| **Application Logs** (Warning) | 30 días | Local SSD / Cloud | gzip |
| **Application Logs** (Error/Critical) | 1 año | Cloud Storage | gzip |
| **Audit Functional** | 7 años | Cloud Storage (WORM) | gzip + encrypt |
| **Audit Security** | 10 años | Cloud Storage (WORM) | gzip + encrypt |
| **Audit Logs (GDPR/Legal)** | 10 años | Cloud Storage (WORM) | gzip + encrypt |
| **Security Events** | 3 años | Cloud Storage (WORM) | gzip + encrypt |
| **Access Logs (Nginx)** | 90 días | Local / Cloud | gzip |
| **PostgreSQL Logs** | 30 días | Local / Cloud | gzip |
| **Hangfire Logs** | 90 días | Local / Cloud | gzip |
| **Nginx Access/Error** | 90 días | Local / Cloud | gzip |
| **PostgreSQL Audit** | 7 años | Cloud (WORM) | gzip + encrypt |

### Almacenamiento por Nivel
| Nivel | Storage Tier | Costo | Acceso |
|-------|--------------|-------|--------|
| **Hot** (< 30 días) | SSD Local / NVMe | Alto | Inmediato |
| **Warm** (30 días - 1 año) | Cloud Standard | Medio | Minutos |
| **Cold** (1-7 años) | Cloud Archive / Glacier | Bajo | Horas |
| **Frozen** (> 7 años) | Cold Storage / Tape | Mínimo | Días |

### Cumplimiento Normativo
| Regulación | Requisito | Implementación |
|------------|-----------|----------------|
| **GDPR** | Derecho al olvido, portabilidad | Logs anonimizados tras 30d, exportación JSON |
| **SOX** | Integridad financiera | Audit logs inmutables 7 años |
| **ISO 27001** | Trazabilidad seguridad | Security logs 3 años |
| **Ley Local (Colombia)** | Datos personales | Logs con datos personales 2 años máx |

---

## Implementación Técnica (Resumen)

### Serilog Configuration (Conceptual)
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "WorkplaceBooking": "Information"
      }
    },
    "Enrich": ["FromLogContext", "WithCorrelationId", "WithTraceId"],
    "WriteTo": [
      { "Name": "Console", "Args": { "formatter": "Serilog.Formatting.Json.JsonFormatter" } },
      { "Name": "File", "Args": { "path": "logs/app-.log", "rollingInterval": "Day", "retainedFileCountLimit": 30 } },
      { "Name": "Seq", "Args": { "serverUrl": "http://seq:5341" } }
    ]
  }
}
```

### Correlation ID Middleware (Conceptual)
```csharp
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                        ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    LogContext.PushProperty("CorrelationId", correlationId);
    await next();
});
```

### Audit Service Interface
```csharp
public interface IAuditService
{
    Task LogAsync(AuditLogEntry entry);
    Task LogDomainEventAsync(IDomainEvent domainEvent, IAggregateRoot entity, CancellationToken ct);
}
```

---

*Documento versión 1.0 | Stack: React 18, .NET 8, PostgreSQL 16, Docker, Microsoft Entra ID*