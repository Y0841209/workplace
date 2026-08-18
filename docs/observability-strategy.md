# Workplace Booking Platform - Observability Strategy

## 1. Objetivos de Observabilidad

| Objetivo | Descripción |
|----------|-------------|
| **Visibilidad end-to-end** | Rastrear solicitudes desde frontend (React) → API (.NET 8) → PostgreSQL → Microsoft Entra ID |
| **Detección proactiva** | Identificar degradación de rendimiento antes de impacto al usuario |
| **Debugging rápido** | Correlacionar logs, métricas y trazas para reducir MTTR |
| **Cumplimiento normativo** | Auditoría completa de reservas, check-ins y accesos para cumplimiento legal |
| **Capacity planning** | Predecir necesidades de escalado basadas en tendencias de uso |
| **Seguridad** | Detectar anomalías de acceso, intentos de intrusión, abuso de recursos |

---

## 2. KPIs Técnicos

### Latencia
| Métrica | Objetivo | Percentil |
|---------|----------|-----------|
| Latencia API (p50) | < 100ms | p50 |
| Latencia API (p95) | < 300ms | p95 |
| Latencia API (p99) | < 500ms | p99 |
| Latencia DB queries (p95) | < 50ms | p95 |
| Tiempo de render inicial (FCP) | < 1.5s | p75 |
| Tiempo interactivo (TTI) | < 3s | p75 |

### Disponibilidad
| Métrica | Objetivo |
|---------|----------|
| Uptime API | 99.9% |
| Uptime Frontend | 99.95% |
| Uptime PostgreSQL | 99.99% |
| Tiempo de recuperación (MTTR) | < 15 min |

### Throughput
| Métrica | Objetivo |
|---------|----------|
| Requests/seg (API) | Soportar 500 RPS sostenidos |
| Reservas concurrentes | 1000 simultáneas |
| Check-ins simultáneos | 200 simultáneos |

### Errores
| Métrica | Objetivo |
|---------|----------|
| Error rate (5xx) | < 0.1% |
| Error rate (4xx) | < 1% |
| Auth failure rate | < 0.5% |

### Recursos
| Métrica | Objetivo |
|---------|----------|
| CPU API (p95) | < 70% |
| Memoria API (p95) | < 80% |
| CPU PostgreSQL (p95) | < 75% |
| Conexiones DB (p95) | < 80% pool |
| Disco (p95) | < 80% |

---

## 3. KPIs Funcionales

### Reservas
| KPI | Descripción | Frecuencia |
|-----|-------------|------------|
| Tasa de conversión búsqueda→reserva | % búsquedas que terminan en reserva | Diaria |
| Tasa de no-show | % reservas CONFIRMED que se convierten en NOT_CHECKED_IN | Diaria |
| Tasa de cancelación | % reservas canceladas vs totales | Diaria |
| Tiempo medio de reserva | Tiempo desde búsqueda hasta confirmación | Diaria |
| Ocupación por tipo de recurso | % ocupación OPEN_WORKSPACE / CLOSED_OFFICE / MEETING_ROOM | Diaria |
| Ocupación por piso | % ocupación por piso (3, 6, 10) | Diaria |
| Reservas por perfil | Distribución por perfil (COLLABORATOR...PARTNER) | Semanal |

### Check-in
| KPI | Descripción | Frecuencia |
|-----|-------------|------------|
| Tasa de check-in | % reservas CONFIRMED que hacen check-in | Diaria |
| Tiempo medio check-in | Tiempo desde inicio reserva hasta check-in | Diaria |
| Check-ins fuera de ventana | % check-ins fuera de ±15 min | Diaria |
| Fallos QR | % escaneos QR inválidos/no coincidentes | Diaria |

### Usuarios
| KPI | Descripción | Frecuencia |
|-----|-------------|------------|
| Usuarios activos diarios (DAU) | Usuarios únicos con actividad | Diaria |
| Usuarios activos mensuales (MAU) | Usuarios únicos mensuales | Mensual |
| Adopción por perfil | % usuarios por perfil (COLLABORATOR...PARTNER) | Semanal |
| Excepciones activas | Usuarios con ReservationException vigentes | Diaria |

### Notificaciones
| KPI | Descripción | Frecuencia |
|-----|-------------|------------|
| Tasa de entrega email | % emails entregados vs encolados | Diaria |
| Tiempo de envío | Latencia p95 encolar→enviar | Diaria |
| Fallos reintento | % notificaciones en FAILED tras reintentos | Diaria |

### Auditoría
| KPI | Descripción | Frecuencia |
|-----|-------------|------------|
| Eventos auditados/día | Volumen total eventos auditados | Diaria |
| Acciones críticas/día | CREATE/DELETE/UPDATE en entidades críticas | Diaria |
| Accesos no autorizados | Intentos 403/401 por día | Diaria |

---

## 4. Logs Requeridos

### Niveles de Log
| Nivel | Uso | Retención |
|-------|-----|-----------|
| **Debug** | Detalle de flujo, payloads, queries SQL | 7 días |
| **Information** | Inicio/fin requests, eventos de negocio, cambios de estado | 30 días |
| **Warning** | Reintentos, timeouts, validaciones fallidas, rate limit hits | 90 días |
| **Error** | Excepciones no controladas, fallos BD, fallos auth, fallos email | 1 año |
| **Critical** | Caída servicio, pérdida datos, brecha seguridad | Indefinida |

### Campos Obligatorios por Log
| Campo | Descripción | Ejemplo |
|-------|-------------|---------|
| `timestamp` | ISO 8601 UTC | `2026-08-16T14:30:00.123Z` |
| `level` | Debug/Information/Warning/Error/Critical | `Information` |
| `message` | Mensaje legible | `Reservation created` |
| `traceId` | W3C Trace Context | `00-abc123...-def456...-01` |
| `spanId` | Identificador de span | `def456...` |
| `userId` | ID usuario autenticado | `11111111-1111-...` |
| `action` | Operación realizada | `CreateReservation` |
| `entityType` | Entidad afectada | `Reservation` |
| `entityId` | ID entidad afectada | `aaa-bbb-ccc` |
| `ipAddress` | IP cliente | `192.168.1.1` |
| `userAgent` | User-Agent cliente | `Mozilla/5.0...` |
| `durationMs` | Duración operación (ms) | `45` |
| `statusCode` | HTTP status | `201` |
| `error` | Detalle error si aplica | `timeout connecting to DB` |

### Categorías de Log por Componente

| Componente | Categorías | Frecuencia Esperada |
|------------|------------|---------------------|
| **Frontend (React)** | Navegación, errores JS, API calls, Web Vitals | Alta |
| **API (.NET)** | HTTP requests, auth, validaciones, DB queries, external calls | Muy alta |
| **PostgreSQL** | Slow queries (>100ms), deadlocks, conexiones, checkpoints | Media |
| **Microsoft Entra ID** | Auth successes/failures, token refresh, MFA challenges | Media |
| **Hangfire** | Job enqueued/started/succeeded/failed, retries | Media |
| **Nginx** | Access logs, rate limit hits, upstream errors | Muy alta |
| **PostgreSQL** | Slow queries, deadlocks, autovacuum, replication lag | Media |

---

## 5. Métricas Requeridas

### Métricas de Aplicación (Prometheus / OpenTelemetry)

#### HTTP Requests
| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `http_requests_total` | Counter | method, path, status_code, user_role | Total requests HTTP |
| `http_request_duration_seconds` | Histogram | method, path, status_code | Latencia requests |
| `http_request_size_bytes` | Histogram | method, path | Tamaño request |
| `http_response_size_bytes` | Histogram | method, path | Tamaño response |

#### Autenticación
| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `auth_attempts_total` | Counter | provider, result (success/failure), error_type | Intentos login |
| `token_refresh_total` | Counter | result (success/failure) | Refresh tokens |
| `active_sessions` | Gauge | - | Sesiones activas |

#### Reservas
| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `reservations_created_total` | Counter | resource_type, user_profile, status | Reservas creadas |
| `reservations_cancelled_total` | Counter | resource_type, reason, actor_role | Reservas canceladas |
| `reservations_active` | Gauge | resource_type, status | Reservas activas actuales |
| `reservation_duration_seconds` | Histogram | resource_type | Duración reservas |
| `checkin_total` | Counter | resource_type, result (success/failure) | Check-ins |
| `checkout_total` | Counter | resource_type | Check-outs |
| `no_show_total` | Counter | resource_type, user_profile | No-shows |
| `overlap_attempts_total` | Counter | resource_type | Intentos reserva superpuesta |
| `capacity_exceeded_attempts_total` | Counter | resource_type | Intentos exceder capacidad |

#### Disponibilidad
| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `resource_availability` | Gauge | resource_id, resource_type, floor | Disponibilidad actual |
| `future_reservations_per_user` | Histogram | user_profile | Reservas futuras por usuario |

#### Notificaciones
| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `notifications_sent_total` | Counter | type, status (sent/failed) | Notificaciones enviadas |
| `notification_latency_seconds` | Histogram | type | Latencia encolar→enviar |
| `notification_retries_total` | Counter | type, attempt | Reintentos |

#### Base de Datos
| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `db_connections_active` | Gauge | pool | Conexiones activas |
| `db_connections_idle` | Gauge | pool | Conexiones idle |
| `db_query_duration_seconds` | Histogram | query_type, table | Latencia queries |
| `db_errors_total` | Counter | error_type, table | Errores DB |
| `db_deadlocks_total` | Counter | - | Deadlocks |
| `db_replication_lag_seconds` | Gauge | - | Replication lag |

#### Cache / Rate Limiting
| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `rate_limit_hits_total` | Counter | zone, result (allowed/denied) | Rate limit hits |
| `cache_hits_total` | Counter | cache_name, result (hit/miss) | Cache hits/misses |

#### Infraestructura
| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `container_cpu_usage` | Gauge | container, pod | CPU por contenedor |
| `container_memory_usage` | Gauge | container, pod | Memoria por contenedor |
| `container_restarts_total` | Counter | container, reason | Reinicios contenedor |
| `pod_status` | Gauge | pod, phase | Estado pods |

---

## 6. Trazabilidad Requerida

### Contexto de Trazabilidad
| Contexto | Propagación | Formato |
|----------|-------------|---------|
| **Frontend → API** | Header `traceparent` (W3C Trace Context) | `traceparent: 00-traceId-spanId-flags` |
| **API → PostgreSQL** | `ApplicationName` en connection string + `Comment` en queries | `App=WorkplaceBooking;TraceId=...` |
| **API → Hangfire** | `TraceId` en job metadata | JSON en job args |
| **API → Email (SMTP)** | `X-Correlation-ID` header | Header personalizado |
| **Frontend → Entra ID** | `state` parameter en OIDC | State parameter OAuth2 |

### Formato Trace Context (W3C)
```
traceparent: 00-<trace-id>-<span-id>-<flags>
tracestate: vendor=value,key=value
```

### Atributos de Span Requeridos (OpenTelemetry)

| Atributo | Requerido | Descripción |
|----------|-----------|-------------|
| `http.method` | Sí | GET, POST, PUT, DELETE |
| `http.target` | Sí | Ruta completa |
| `http.status_code` | Sí | 200, 404, 500 |
| `http.route` | Sí | Plantilla ruta: `/api/v1/reservations/{id}` |
| `http.request_content_length` | No | Bytes request |
| `http.response_content_length` | No | Bytes response |
| `net.peer.ip` | Sí | IP cliente |
| `http.user_agent` | No | User-Agent |
| `enduser.id` | Sí | User ID autenticado |
| `enduser.role` | No | Roles usuario |
| `db.system` | Sí | `postgresql` |
| `db.operation` | Sí | SELECT, INSERT, UPDATE, DELETE |
| `db.sql.table` | Sí | Tabla afectada |
| `db.statement` | No | Query SQL (sanitizada) |
| `db.operation_batch_size` | No | Batch size |
| `messaging.system` | Sí | `hangfire` |
| `messaging.destination` | Sí | Queue name |
| `messaging.operation` | Sí | `send` / `receive` |

### Correlación Requerida

| Flujo | Correlación |
|-------|-------------|
| **Frontend → API** | `traceparent` header propagado |
| **API → DB** | `TraceId` en `ApplicationName` / `Comment` query |
| **API → Hangfire** | `TraceId` en job metadata |
| **API → SMTP** | `X-Correlation-ID` header |
| **Frontend → Entra ID** | `state` param OIDC |
| **Nginx → API** | `X-Request-ID` / `X-Correlation-ID` |

### Sampling Strategy
| Escenario | Sampling Rate |
|-----------|---------------|
| Errors (5xx) | 100% |
| Latencia > p99 | 100% |
| Auth failures | 100% |
| High-value transactions (reservas, check-ins) | 100% |
| Normal traffic | 10-20% |
| Health checks | 0% |

### Exportación
| Destino | Protocolo | Formato |
|---------|-----------|---------|
| OpenTelemetry Collector | OTLP/gRPC | Protobuf |
| Prometheus | HTTP Pull | Prometheus Exposition |
| Loki (logs) | HTTP Push | JSON |
| Tempo (traces) | OTLP/gRPC | Protobuf |

---

*Documento versión 1.0 | Contexto: React 18, .NET 8, PostgreSQL 16, Docker, Microsoft Entra ID*