# Análisis FRD - Workplace Booking Platform

## 1. Resumen Ejecutivo

**Workplace Booking Platform** es una aplicación web responsiva para la reserva de espacios de trabajo (oficinas abiertas, oficinas cerradas y salas de juntas) orientada a una firma legal. La solución prioriza seguridad, trazabilidad, mínimo privilegio, auditoría y una experiencia sobria alineada con la identidad corporativa.

### Alcance del MVP
- Reserva, modificación y cancelación de espacios por horas
- Disponibilidad en tiempo real con validación de conflictos
- QR y check-in para oficinas (abiertas y cerradas)
- Notificaciones por correo corporativo (confirmación, modificación, cancelación, recordatorio 15 min)
- RBAC + perfiles de negocio + roles administrativos
- Auditoría completa de eventos sensibles
- Despliegue en Ubuntu 24.04 con Docker Compose + Nginx

### Stack Tecnológico Definido
| Capa | Tecnología |
|------|------------|
| Frontend | React + TypeScript + Material UI |
| Backend | .NET 8 Web API + Entity Framework Core + FluentValidation + Serilog |
| Base de Datos | PostgreSQL 16 |
| Identidad | Microsoft Entra ID (OIDC) |
| Infraestructura | Ubuntu 24.04 + Docker Compose + Nginx |
| CI/CD | GitHub Actions |
| Reportes | Power BI |

### Inventario Inicial (Sede Principal - Bogotá)
| Piso | Oficinas Abiertas | Oficinas Cerradas | Salas de Juntas |
|------|-------------------|-------------------|-----------------|
| 3 | 30 | 9 | SJ-06, SJ-07 (cap. 8 c/u) |
| 6 | 18 | 10 | SJ-01 a SJ-05 (cap. 5-24) |
| 10 | 12 | 5 | N/A |
| **Total** | **60** | **24** | **7** |

---

## 2. Módulos Funcionales

| Módulo | Descripción | Componentes Clave |
|--------|-------------|-------------------|
| **Gestión de Inventario** | CRUD de sedes, pisos, zonas, tipos de recurso y recursos reservables | Locations, Floors, Zones, ResourceTypes, Resources |
| **Motor de Reservas** | Crear, modificar, cancelar reservas; validación de disponibilidad y reglas de negocio | Reservations, Availability Engine, Business Rules |
| **Control de Acceso (RBAC/ABAC)** | Perfiles de negocio + roles administrativos + políticas por tipo de recurso | BusinessProfiles, ApplicationRoles, ResourceAccessPolicies, ReservationExceptions |
| **QR & Check-in** | Generación QR por recurso, escaneo móvil, validación backend, check-in/check-out | Resources (public_qr_id), CheckIns, QR Resolution API |
| **Notificaciones** | Outbox pattern para envío asíncrono de correos transaccionales y recordatorios | NotificationOutbox, Background Worker, Email Service |
| **Auditoría** | Registro inmutable de eventos sensibles con actor, acción, entidad, antes/después | AuditLogs, Middleware, Domain Events |
| **Administración** | Gestión de usuarios, roles, excepciones, recursos, reportes, auditoría | Admin Panel, Bulk Import, User Management |
| **Reportes & BI** | Métricas de ocupación, uso, conflictos, tendencias | Power BI (DirectQuery sobre PostgreSQL) |

---

## 3. Casos de Uso

### Actores Principales
| Actor | Descripción | Permisos Base |
|-------|-------------|---------------|
| **Usuario Autenticado (USER)** | Colaborador, Asociado, Líder, Director, Socio | Reservas propias, check-in, ver disponibilidad |
| **ROOM_ADMIN** | Administrador de salas | Reservas ilimitadas en MEETING_ROOM, gestión salas |
| **SUPPORT** | Soporte TI | Modificar reservas ajenas (con motivo), consultar auditoría |
| **GLOBAL_ADMIN** | Administrador global | Control total: recursos, usuarios, roles, excepciones, auditoría |
| **Anónimo** | Usuario sin autenticar | Solo login / resolución QR pública |

### Casos de Uso por Módulo

#### Gestión de Inventario (Admin)
- **UC-INV-01**: Registrar sede, pisos, zonas
- **UC-INV-02**: CRUD de recursos (oficinas/salas) con capacidad, QR, tipo
- **UC-INV-03**: Carga masiva de recursos (import CSV/Excel)
- **UC-INV-04**: Activar/desactivar recursos (mantenimiento)

#### Motor de Reservas (Usuario)
- **UC-RES-01**: Buscar disponibilidad por tipo, piso, fecha, horario, capacidad
- **UC-RES-02**: Crear reserva (validar perfil, disponibilidad, límites, reglas temporales)
- **UC-RES-03**: Modificar reserva propia (validar mismo día, sin conflictos, límites)
- **UC-RES-04**: Cancelar reserva propia (sin penalización)
- **UC-RES-05**: Ver mis reservas (activas, histórico, próximas)
- **UC-RES-06**: Soporte modifica reserva ajena (con motivo trazable)

#### Control de Acceso (Admin)
- **UC-ACC-01**: Asignar/remover perfiles de negocio a usuarios (vigencia)
- **UC-ACC-02**: Asignar/remover roles administrativos (vigencia)
- **UC-ACC-03**: Configurar políticas de acceso por perfil × tipo recurso
- **UC-ACC-04**: Crear excepciones temporales de límite de reservas (por usuario, tipo recurso, fechas)

#### QR & Check-in (Usuario)
- **UC-QR-01**: Escanear QR en oficina → resolver recurso → ver reserva activa → confirmar check-in
- **UC-QR-02**: Validaciones backend: usuario autenticado, reserva vigente, recurso correcto, ventana ±15 min
- **UC-QR-03**: Check-out automático al finalizar horario o manual

#### Notificaciones (Sistema)
- **UC-NOT-01**: Enviar correo al crear/modificar/cancelar reserva (inmediato)
- **UC-NOT-02**: Enviar recordatorio 15 min antes de inicio (worker programado)
- **UC-NOT-03**: Notificar a usuario afectado por modificación de Soporte

#### Auditoría (Admin/Sistema)
- **UC-AUD-01**: Registrar eventos sensibles (login, CRUD reservas, cambios roles, excepciones, admin actions)
- **UC-AUD-02**: Consultar auditoría con filtros (actor, acción, entidad, rango fechas)

---

## 4. Riesgos Técnicos

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| **Doble reserva por concurrencia** | Alta | Crítico | Exclusion constraints en PostgreSQL (GIST + tsrange) a nivel BD + validación optimista en API |
| **Perfiles mal asignados / escalada de privilegios** | Media | Alto | Validación server-side en cada request; auditoría de cambios de roles/perfiles; principio mínimo privilegio |
| **Exposición de ocupación / datos sensibles** | Media | Alto | HTTPS obligatorio; CSP estricto; no mostrar nombre ocupante a usuarios sin permiso; logs sin PII |
| **Uso incorrecto de QR (falso check-in, replay)** | Media | Medio | QR = UUID aleatorio (sin credenciales); validación backend de reserva activa + ventana temporal + ownership; rate limiting en endpoint QR |
| **Límites de reservas bypassados** | Baja | Alto | Validación en BD (trigger) + validación en API; ROOM_ADMIN solo MEETING_ROOM; GLOBAL_ADMIN auditable |
| **Fallas de notificación (correo no llega)** | Media | Medio | Outbox pattern (transaccional); reintentos con backoff exponencial; dead-letter queue; alertas en fallos persistentes |
| **Crecimiento multi-sede no previsto** | Baja | Medio | Modelo de datos preparado para multi-sede (location_id en todas las entidades); Índices por location |
| **Performance en búsqueda disponibilidad** | Media | Medio | Índices compuestos (resource_id + date + status); exclusion constraints optimizados; paginación |
| **Migración Entra ID → sincronización perfiles** | Media | Bajo | Diseño desacoplado: perfiles gestionados en app inicialmente; tabla user_business_profiles preparada para sync externa |
| **Dependencia Microsoft Entra ID (vendor lock-in)** | Baja | Medio | Abstracción ICurrentUserService; solo claims estándar (sub, email, name, groups); adaptable a otro OIDC |

---

## 5. Dependencias

### Dependencias Externas (Runtime)
| Dependencia | Propósito | Criticidad |
|-------------|-----------|------------|
| **Microsoft Entra ID** | Autenticación OIDC, claims usuario, grupos | Crítica - Sin auth no funciona |
| **PostgreSQL 16** | Persistencia transaccional, constraints, triggers | Crítica |
| **SMTP / Email Provider** | Envío notificaciones (Office 365 / SendGrid / SMTP interno) | Alta |
| **Power BI** | Reportes ejecutivos (opcional para MVP) | Media |
| **Nginx** | Reverse proxy, TLS termination, rate limiting, static files | Crítica |
| **Docker Engine / Compose** | Orquestación contenedores | Crítica |

### Dependencias de Desarrollo (Build/CI)
| Dependencia | Propósito |
|-------------|-----------|
| **.NET 8 SDK** | Build backend, EF Core tools, tests |
| **Node.js 20+** | Build frontend, npm, Vite, tests |
| **GitHub Actions** | CI/CD pipelines |
| **CodeQL / SonarQube** | SAST |
| **Dependabot** | SCA (vulnerabilidades dependencias) |
| **OWASP ZAP** | DAST en QA |
| **k6 / NBomber** | Pruebas de carga/concurrencia |

### Dependencias Internas (Entre Proyectos - Clean Architecture)

```
WorkplaceBooking.API (Presentation)
    ↓ depends on
WorkplaceBooking.Application (Use Cases, DTOs, Interfaces)
    ↓ depends on
WorkplaceBooking.Domain (Entities, Value Objects, Domain Events, Rules)
    ↓ depends on
WorkplaceBooking.SharedKernel (Primitives, Base Classes, Common Types)

WorkplaceBooking.Infrastructure (Implementations)
    ↓ implements interfaces from
WorkplaceBooking.Application
    ↓ uses
WorkplaceBooking.Domain
    ↓ uses
WorkplaceBooking.SharedKernel
```

### Reglas de Dependencia (Clean Architecture)
| Permitido | Prohibido |
|-----------|-----------|
| API → Application | Application → API |
| Application → Domain | Domain → Application/Infrastructure/API |
| Application → SharedKernel | Infrastructure → API |
| Infrastructure → Application/Domain/SharedKernel | Domain → Infrastructure |
| Tests → Cualquier capa | SharedKernel → Otras capas |

---

## Decisiones Arquitectónicas Clave (Para ADRs)

1. **Exclusion Constraints en PostgreSQL** para prevención de doble reserva a nivel BD (race-condition proof)
2. **Transactional Outbox Pattern** para notificaciones confiables sin dual-write
3. **QR con UUID aleatorio** (sin credenciales) + validación backend completa
4. **Hybrid Authorization**: RBAC (roles admin) + ABAC (perfiles negocio + políticas recurso)
5. **Clean Architecture** estricta con 4 capas + SharedKernel
6. **Multi-sede ready** desde el modelo de datos (location_id ubiquo)
7. **OIDC-only auth** (Entra ID), sin passwords locales
7. **Audit Log inmutable** con before/after values + correlation ID
8. **Mobile-first responsive** con Material UI + breakpoints definidos
9. **Docker Compose** para dev/prod parity, Nginx como edge único
10. **GitHub Actions** con SAST/SCA/DAST + tests pirámide