# Arquitectura Lógica - Workplace Booking Platform

## 1. Componentes Principales

| Componente | Tipo | Capa |
|------------|------|------|
| Frontend | SPA (Single Page Application) | Presentación |
| Backend | Web API REST | Aplicación |
| Base de datos | Base de datos relacional | Datos |
| Identidad | Proveedor de identidad (IdP) | Seguridad transversal |

---

## 2. Responsabilidades

### Frontend (React + TypeScript + Material UI)
- Renderizar interfaz de usuario responsive (mobile-first)
- Gestión de estado local y de servidor (React Query / Context)
- Navegación SPA (React Router)
- Formularios con validación client-side (React Hook Form + Zod)
- Autenticación silenciosa y renovación de tokens (OIDC PKCE)
- Componentes UI: listas de recursos, calendarios, formularios de reserva, panel de administración, check-in QR
- Internacionalización (es-CO) y tema corporativo (#FFD800, #0E0E0E)
- Accesibilidad WCAG AA

### Backend (.NET 8 + ASP.NET Core Web API)
- Exponer API REST versionada (/api/v1/)
- Autenticación JWT Bearer (validación tokens Entra ID)
- Autorización: policies basadas en roles (USER, ROOM_ADMIN, SUPPORT, GLOBAL_ADMIN) y claims de perfiles de negocio
- Reglas de negocio: disponibilidad, conflictos, límites, validaciones temporales
- Orquestación de casos de uso (CQRS: Commands / Queries via MediatR)
- Validación de entrada (FluentValidation)
- Auditoría automática via middleware + domain events
- Publicación de eventos de dominio para notificaciones (outbox pattern)
- Health checks (liveness / readiness)
- OpenAPI/Swagger + Scalar para documentación

### Base de datos (PostgreSQL 16)
- Esquema `booking` con 15 tablas principales
- Constraints declarativos: CHECK, UNIQUE, EXCLUSION (GIST + tsrange) para doble reserva
- Triggers: `updated_at`, `validate_reservation_business_rules`, `validate_checkin_business_rules`
- Funciones SQL: `user_has_active_role`, `user_can_reserve_resource`
- Índices optimizados para consultas de disponibilidad y auditoría
- Tipos enumerados: `reservation_status`, `notification_status`, `notification_type`, `checkin_method`
- Extensiones: `pgcrypto`, `btree_gist`, `citext`

### Identidad (Microsoft Entra ID)
- Autenticación OIDC Authorization Code Flow + PKCE (SPA público)
- Emisión de Access Token (JWT) e ID Token
- Claims estándar: `sub`/`oid`, `email`, `name`, `jobTitle`, `department`, `groups`
- Conditional Access, MFA, Identity Protection (configurados en tenant)
- JWKS endpoint para validación de firma en backend
- Logout único (RP-Initiated + Front-channel)

---

## 3. Relaciones entre Componentes

```text
┌─────────────────┐     HTTPS/OIDC      ┌──────────────────┐
│    Frontend     │ ◄─────────────────► │ Microsoft Entra  │
│  (React SPA)    │   Auth Code + PKCE  │       ID         │
└────────┬────────┘                     └──────────────────┘
         │
         │ HTTPS / REST (JWT Bearer)
         ▼
┌─────────────────┐     TCP/PostgreSQL    ┌──────────────────┐
│    Backend      │ ◄───────────────────► │  PostgreSQL 16   │
│ (.NET 8 Web API)│   Entity Framework    │   (esquema       │
└─────────────────┘     Core / Dapper     │    booking)      │
         │                                    └──────────────────┘
         │
         │ SMTP / HTTP
         ▼
┌──────────────────┐
│  Email Provider  │
│ (Office 365 /    │
│  SendGrid / SMTP)│
└──────────────────┘
```

### Flujos Principales

| Flujo | Secuencia |
|-------|-----------|
| **Login** | Frontend → Entra ID (redirect) → Entra ID → Frontend (code) → Frontend → Entra ID (token endpoint) → Entra ID → Frontend (tokens) → Frontend → Backend (validación opcional) |
| **Crear reserva** | Frontend → Backend (POST /reservations + JWT) → Backend valida token → Backend verifica políticas/perfiles → Backend consulta BD disponibilidad → Backend inserta reserva (exclusion constraint) → Backend retorna 201 → Backend publica evento `ReservationCreated` → Worker procesa outbox → Email |
| **Check-in QR** | Usuario escanea QR → Frontend (página pública) → Backend (GET /check-in/{qrId}) → Backend resuelve recurso + reserva activa → Frontend muestra botón → Usuario confirma → Backend (POST /check-in) → Backend valida ownership/ventana/recurso → Backend inserta checkin + actualiza reserva → 200 |
| **Recordatorio** | Worker (cron 15 min) → Lee `notification_outbox` PENDING con `scheduled_at <= now()` → Envía emails → Marca SENT/FAILED → Reintentos con backoff |

---

## 4. Dependencias Permitidas

| Desde | Hacia | Tipo |
|-------|-------|------|
| Frontend | Backend | REST API (HTTPS + JWT) |
| Frontend | Entra ID | OIDC (Auth Code + PKCE) |
| Backend | Base de datos | EF Core / ADO.NET (connection pool) |
| Backend | Entra ID | Validación JWT (JWKS) |
| Backend | Email Provider | SMTP / HTTP API |
| Backend | SharedKernel | Librería interna (primitivos, Result, Exceptions) |
| Application | Domain | Referencia de proyecto |
| Application | SharedKernel | Referencia de proyecto |
| Infrastructure | Application | Implementa interfaces (DI) |
| Infrastructure | Domain | Referencia de proyecto |
| Infrastructure | SharedKernel | Referencia de proyecto |
| API | Application | Referencia de proyecto (MediatR) |
| API | Infrastructure | Registro DI (solo en Composition Root) |
| Tests | Cualquier capa | Referencia para testing |

---

## 5. Dependencias Prohibidas

| Regla | Descripción |
|-------|-------------|
| **Domain** | No referencia a Application, Infrastructure, API, SharedKernel (solo .NET BCL) |
| **Application** | No referencia a Infrastructure, API, Frontend |
| **Infrastructure** | No referencia a API, Frontend |
| **API** | No instancia directamente repositorios/EF Core (usa Application via MediatR) |
| **Frontend** | No accede directo a Base de datos, Email, Entra ID token endpoint (solo via Backend proxy o Auth Code flow) |
| **SharedKernel** | No referencia a ninguna otra capa del proyecto |
| **Base de datos** | No lógica de negocio en triggers complejos (solo constraints, updated_at, validaciones atómicas) |
| **Entra ID** | Backend no usa client credentials flow para APIs propias (solo validación JWT) |

---

## Reglas de Arquitectura Limpia (Clean Architecture)

1. **Dirección de dependencias**: Apuntan siempre hacia el centro (Domain)
2. **Domain**: Puro, sin dependencias externas, contiene reglas de negocio invariantes
3. **Application**: Orquesta casos de uso, define interfaces (puertos), no conoce implementaciones
4. **Infrastructure**: Implementa interfaces de Application (adaptadores), conoce detalles técnicos
5. **API**: Capa delgada, solo mapping HTTP → Commands/Queries, serialización, middleware
6. **Frontend**: Independiente del backend, comparte solo contrato OpenAPI / tipos TypeScript generados