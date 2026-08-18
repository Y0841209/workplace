# Workplace Booking Platform - Architecture Diagrams

## 1. System Context Diagram (C4 Level 1)

```mermaid
C4Context
title System Context - Workplace Booking Platform

Person(user, "User", "Employee / Admin")
Person(admin, "Admin", "GLOBAL_ADMIN / ROOM_ADMIN / SUPPORT")
System(booking, "Workplace Booking Platform", "Responsive web app for workspace reservations")
System_Ext(entra, "Microsoft Entra ID", "Corporate identity provider (OIDC)")
System_Ext(email, "Email Service", "SMTP / SendGrid / Office 365")
System_Ext(powerbi, "Power BI", "Business intelligence & reporting")
System_Ext(qr, "QR Scanner", "Mobile camera / QR reader app")

Rel(user, booking, "Reserve, check-in, view availability", "HTTPS")
Rel(admin, booking, "Manage resources, users, audit", "HTTPS")
Rel(booking, entra, "Authenticate users, get claims", "OIDC/OAuth 2.0")
Rel(booking, email, "Send notifications", "SMTP/API")
Rel(booking, powerbi, "Export data for reports", "Direct Query / Dataset")
Rel(user, qr, "Scan QR code", "Camera")
Rel(qr, booking, "Resolve QR → Check-in", "HTTPS")
```

## 2. Container Diagram (C4 Level 2)

```mermaid
C4Container
title Container Diagram - Workplace Booking Platform

Container_Boundary(c0, "Workplace Booking Platform") {
    Container(frontend, "React SPA", "TypeScript, Material UI", "Responsive web UI, mobile-first")
    Container(api, ".NET 8 Web API", "C#, ASP.NET Core", "REST API, business logic, auth")
    ContainerDb(db, "PostgreSQL 16", "SQL", "Transactional data, audit logs, outbox")
    Container(worker, "Background Worker", ".NET 8, Hangfire", "Notifications, reminders, cleanup")
    Container(nginx, "Nginx", "Reverse Proxy", "SSL termination, routing, rate limiting")
}

Container_Ext(entra, "Microsoft Entra ID", "OIDC Provider", "Authentication & user claims")
Container_Ext(email, "Email Service", "SMTP/API", "Transactional emails")
Container_Ext(powerbi, "Power BI", "BI Platform", "Reporting & dashboards")

Rel(frontend, api, "API calls", "HTTPS/JSON")
Rel(api, db, "Read/Write", "PostgreSQL Protocol")
Rel(worker, db, "Read/Write", "PostgreSQL Protocol")
Rel(worker, email, "Send emails", "SMTP/API")
Rel(nginx, frontend, "Static files + SPA fallback", "HTTP")
Rel(nginx, api, "Proxy /api/*", "HTTP")
Rel(frontend, entra, "Login redirect", "OIDC Auth Code + PKCE")
Rel(api, entra, "Token validation", "OIDC/JWT")
Rel(powerbi, db, "Direct Query", "PostgreSQL Protocol")
```

## 3. Component Diagram - Backend (C4 Level 3)

```mermaid
C4Component
title Component Diagram - Backend API

Container_Boundary(api, ".NET 8 Web API") {
    Component(controllers, "Controllers", "ASP.NET Core", "HTTP endpoints, model binding")
    Component(mediatr, "MediatR", "Library", "CQRS mediator, pipeline behaviors")
    Component(useCases, "Use Cases", "Application Layer", "Commands & Queries handlers")
    Component(validators, "Validators", "FluentValidation", "Input validation")
    Component(domain, "Domain Model", "Domain Layer", "Entities, VOs, Events, Rules")
    Component(repos, "Repositories", "Infrastructure", "EF Core implementations")
    Component(efCore, "EF Core DbContext", "Infrastructure", "ORM, migrations, UoW")
    Component(identity, "Entra ID Auth", "Infrastructure", "JWT validation, claims mapping")
    Component(emailSvc, "Email Service", "Infrastructure", "SMTP client, templates")
    Component(audit, "Audit Middleware", "API", "Request/response logging")
    Component(health, "Health Checks", "ASP.NET Core", "Liveness/readiness probes")
}

ContainerDb(db, "PostgreSQL 16", "Database", "booking schema")

Rel(controllers, mediatr, "Send Command/Query")
Rel(mediatr, useCases, "Dispatch to handlers")
Rel(useCases, validators, "Validate input")
Rel(useCases, domain, "Execute business logic")
Rel(useCases, repos, "Persist/Retrieve")
Rel(repos, efCore, "LINQ / SQL")
Rel(efCore, db, "Execute queries")
Rel(controllers, identity, "Authorize requests")
Rel(useCases, emailSvc, "Queue notifications")
Rel(useCases, audit, "Log sensitive actions")
```

## 4. Component Diagram - Frontend

```mermaid
C4Component
title Component Diagram - Frontend (React)

Container(frontend, "React SPA", "TypeScript, Material UI")

Component(pages, "Pages", "Route Components", "Dashboard, Book, MyReservations, Admin")
Component(layouts, "Layouts", "Layout Components", "MainLayout, AuthLayout, AdminLayout")
Component(components, "UI Components", "Reusable Components", "ResourceCard, ReservationForm, QRScanner")
Component(hooks, "Custom Hooks", "Business Logic", "useReservations, useResources, useAuth")
Component(query, "TanStack Query", "Server State", "Caching, invalidation, mutations")
Component(apiClient, "API Client", "Axios", "HTTP client, interceptors, auth")
Component(authCtx, "Auth Context", "Client State", "User, roles, permissions, login/logout")
Component(themeCtx, "Theme Context", "Client State", "MUI theme, dark mode, palette")
Component(router, "React Router", "Routing", "Protected routes, navigation")
Component(forms, "React Hook Form", "Forms", "Validation with Zod schemas")

Rel(pages, layouts, "Render within")
Rel(pages, components, "Compose")
Rel(pages, hooks, "Consume")
Rel(hooks, query, "Fetch/Mutate")
Rel(query, apiClient, "HTTP requests")
Rel(apiClient, authCtx, "Attach tokens")
Rel(pages, authCtx, "Access user/perms")
Rel(pages, themeCtx, "Access theme")
Rel(router, pages, "Route to")
Rel(components, forms, "Form handling")
```

## 5. Database ER Diagram

```mermaid
erDiagram
    LOCATIONS ||--o{ FLOORS : has
    FLOORS ||--o{ ZONES : contains
    FLOORS ||--o{ RESOURCES : hosts
    ZONES ||--o{ RESOURCES : groups
    RESOURCE_TYPES ||--o{ RESOURCES : categorizes
    
    APP_USERS ||--o{ USER_BUSINESS_PROFILES : has
    BUSINESS_PROFILES ||--o{ USER_BUSINESS_PROFILES : assigned
    APP_USERS ||--o{ USER_APPLICATION_ROLES : has
    APPLICATION_ROLES ||--o{ USER_APPLICATION_ROLES : assigned
    APP_USERS ||--o{ RESERVATIONS : creates
    APP_USERS ||--o{ RESERVATIONS : owns
    APP_USERS ||--o{ CHECKINS : performs
    APP_USERS ||--o{ NOTIFICATION_OUTBOX : receives
    APP_USERS ||--o{ AUDIT_LOGS : acts_as
    
    RESOURCES ||--o{ RESERVATIONS : booked_via
    RESOURCES ||--o{ CHECKINS : checked_into
    RESOURCE_TYPES ||--o{ RESOURCE_ACCESS_POLICIES : governs
    BUSINESS_PROFILES ||--o{ RESOURCE_ACCESS_POLICIES : permits
    
    RESERVATIONS ||--|| CHECKINS : generates
    RESERVATIONS ||--o{ NOTIFICATION_OUTBOX : triggers
    RESERVATION_EXCEPTIONS }|--|| APP_USERS : grants
    RESOURCE_TYPES ||--o{ RESERVATION_EXCEPTIONS : applies_to
    
    APP_SETTINGS {
        uuid id PK
        int maximum_future_active_reservations
        int maximum_advance_days
        int minimum_duration_minutes
        time latest_end_time
        int reminder_minutes_before
        bool allow_cross_day_booking
        bool show_occupant_name_to_users
    }
    
    LOCATIONS {
        uuid id PK
        string code UK
        string name
        string city
        string country
        string timezone
        bool active
    }
    
    FLOORS {
        uuid id PK
        uuid location_id FK
        int floor_number
        string code
        string name
        bool active
    }
    
    ZONES {
        uuid id PK
        uuid floor_id FK
        string code
        string name
        bool active
    }
    
    RESOURCE_TYPES {
        string code PK
        string name
        bool qr_required
        bool checkin_required
        bool active
    }
    
    RESOURCES {
        uuid id PK
        string code UK
        string name
        string resource_type_code FK
        uuid location_id FK
        uuid floor_id FK
        uuid zone_id FK
        int capacity
        uuid public_qr_id UK
        int qr_version
        bool active
        bool reservable
    }
    
    APP_USERS {
        uuid id PK
        uuid entra_object_id UK
        citext email UK
        string display_name
        string job_title
        string department
        bool active
        timestamptz last_login_at
    }
    
    BUSINESS_PROFILES {
        string code PK
        string name
        bool active
    }
    
    APPLICATION_ROLES {
        string code PK
        string name
        string description
        bool active
    }
    
    USER_BUSINESS_PROFILES {
        uuid id PK
        uuid user_id FK
        string profile_code FK
        date valid_from
        date expires_at
        bool active
    }
    
    USER_APPLICATION_ROLES {
        uuid id PK
        uuid user_id FK
        string role_code FK
        date valid_from
        date expires_at
        bool active
    }
    
    RESOURCE_ACCESS_POLICIES {
        uuid id PK
        string resource_type_code FK
        string business_profile_code FK
        bool can_view
        bool can_reserve
        bool can_modify_own
        bool active
    }
    
    RESERVATION_EXCEPTIONS {
        uuid id PK
        uuid user_id FK
        int maximum_future_active_reservations
        string applies_to_resource_type_code FK
        date valid_from
        date expires_at
        string reason
        bool active
    }
    
    RESERVATIONS {
        uuid id PK
        uuid resource_id FK
        uuid user_id FK
        uuid created_by_user_id FK
        date reservation_date
        time start_time
        time end_time
        reservation_status status
        string title
        string description
        int attendee_count
        string support_change_reason
        timestamptz checked_in_at
        timestamptz checked_out_at
        timestamptz cancelled_at
        uuid cancelled_by_user_id FK
        string cancellation_reason
    }
    
    CHECKINS {
        uuid id PK
        uuid reservation_id FK UK
        uuid resource_id FK
        uuid user_id FK
        checkin_method method
        uuid scanned_public_qr_id
        timestamptz checked_in_at
        inet ip_address
        string user_agent
    }
    
    NOTIFICATION_OUTBOX {
        uuid id PK
        uuid reservation_id FK
        uuid recipient_user_id FK
        citext recipient_email
        notification_type type
        string subject
        string body
        timestamptz scheduled_at
        timestamptz sent_at
        notification_status status
        int retry_count
        string last_error
    }
    
    AUDIT_LOGS {
        uuid id PK
        uuid actor_user_id FK
        string action
        string entity_name
        uuid entity_id
        jsonb before_value
        jsonb after_value
        string reason
        inet ip_address
        string user_agent
        uuid correlation_id
        timestamptz created_at
    }
```

## 6. Sequence Diagram - Create Reservation

```mermaid
sequenceDiagram
    actor User
    participant Frontend
    participant API
    participant Domain
    participant DB
    participant EmailWorker
    
    User->>Frontend: Select resource, date, time
    Frontend->>API: GET /api/v1/availability?type=...&date=...&start=...&end=...
    API->>DB: Query available resources (exclusion constraints)
    DB-->>API: Available resources
    API-->>Frontend: List of available resources
    Frontend-->>User: Show options
    
    User->>Frontend: Confirm reservation
    Frontend->>API: POST /api/v1/reservations {resourceId, date, start, end}
    API->>API: Validate JWT, extract userId
    API->>Domain: CreateReservationCommand
    Domain->>Domain: Check business profile permissions
    Domain->>Domain: Check future reservation limit (max 5)
    Domain->>Domain: Check ROOM_ADMIN exception (meeting rooms only)
    Domain->>DB: INSERT reservation (exclusion constraint check)
    alt Conflict
        DB-->>Domain: Exclusion constraint violation
        Domain-->>API: ConflictError
        API-->>Frontend: 409 Conflict
        Frontend-->>User: Show error
    else Success
        DB-->>Domain: Reservation created
        Domain->>Domain: Raise ReservationCreatedEvent
        Domain-->>API: ReservationResult
        API-->>Frontend: 201 Created + Reservation
        Frontend-->>User: Show confirmation
        
        par Async Notification
            Domain->>DB: INSERT notification_outbox (RESERVATION_CREATED)
            EmailWorker->>DB: Poll PENDING notifications
            EmailWorker->>EmailWorker: Render email template
            EmailWorker->>EmailService: Send email
            EmailService-->>EmailWorker: Sent/Failed
            EmailWorker->>DB: UPDATE notification_outbox (SENT/FAILED)
        end
    end
```

## 7. Sequence Diagram - QR Check-in

```mermaid
sequenceDiagram
    actor User
    participant MobileBrowser
    participant Frontend (QR Page)
    participant API
    participant Domain
    participant DB
    
    User->>MobileBrowser: Scan QR code on office door
    MobileBrowser->>Frontend: GET /check-in/{publicQrId}
    Frontend->>API: GET /api/v1/check-in/resources/{publicQrId}
    API->>DB: SELECT resource, active reservation NOW()
    DB-->>API: Resource + Reservation (if exists)
    API-->>Frontend: Resource info + Reservation details
    Frontend-->>MobileBrowser: Show "Check-in" button (if valid)
    
    User->>MobileBrowser: Tap "Check-in"
    MobileBrowser->>Frontend: POST /api/v1/reservations/{id}/check-in
    Frontend->>API: POST /api/v1/reservations/{id}/check-in
    API->>API: Validate JWT, extract userId
    API->>Domain: CheckInCommand
    Domain->>Domain: Validate: user owns reservation
    Domain->>Domain: Validate: resource matches QR
    Domain->>Domain: Validate: within time window (±15 min)
    Domain->>Domain: Validate: resource type requires check-in
    Domain->>DB: INSERT checkins (unique reservation_id)
    alt Already checked in / Invalid
        DB-->>Domain: Constraint violation / Business rule fail
        Domain-->>API: Error
        API-->>Frontend: 400/409
        Frontend-->>MobileBrowser: Show error
    else Success
        DB-->>Domain: Check-in recorded
        Domain->>Domain: Update reservation status → CHECKED_IN
        Domain->>Domain: Raise CheckInCompletedEvent
        Domain-->>API: CheckInResult
        API-->>Frontend: 200 OK
        Frontend-->>MobileBrowser: Show success + checkout info
    end
```

## 8. Deployment Diagram

```mermaid
C4Deployment
title Deployment Diagram - Production (Ubuntu 24.04 VM)

Deployment_Node(vm, "Ubuntu 24.04 VM", "VM / Bare Metal") {
    Container(nginx, "Nginx", "Reverse Proxy", "Ports 80, 443")
    Container(frontend, "Frontend", "Static Files + Nginx", "Served by Nginx")
    Container(api, "API", ".NET 8 Kestrel", "Port 5000 (internal)")
    Container(worker, "Worker", ".NET 8 Console", "Background jobs")
    Container(db, "PostgreSQL 16", "Database", "Port 5432 (internal)")
    Container_Ext(entra, "Microsoft Entra ID", "Cloud", "HTTPS")
    Container_Ext(email, "Email Service", "Cloud/On-prem", "SMTP/API")
    Container_Ext(backup, "Backup Storage", "S3/Blob", "WAL + Base backups")
}

Rel(nginx, frontend, "Serves static", "Local FS")
Rel(nginx, api, "Proxies /api/*", "localhost:5000")
Rel(api, db, "PostgreSQL Protocol", "localhost:5432")
Rel(worker, db, "PostgreSQL Protocol", "localhost:5432")
Rel(worker, email, "SMTP/HTTP", "Internet")
Rel(api, entra, "OIDC Discovery + JWKS", "HTTPS")
Rel(db, backup, "pg_basebackup + WAL", "Network")
```

## 9. Data Flow - Notification Processing

```mermaid
flowchart TD
    A[Business Event\n(Reservation Created/\nModified/Cancelled)] --> B{Domain Event\nRaised}
    B --> C[Insert into\nnotification_outbox\nPENDING]
    C --> D[Background Worker\n(Polls every 60s)]
    D --> E{Type == REMINDER?}
    E -->|Yes| F[Check scheduled_at\n<= NOW + 15min]
    E -->|No| G[Process Immediately]
    F --> H{Due?}
    H -->|Yes| G
    H -->|No| I[Skip this cycle]
    G --> J[Render Template\nwith Reservation Data]
    J --> K[Send via Email Service]
    K --> L{Success?}
    L -->|Yes| M[UPDATE status=SENT\nsent_at=NOW]
    L -->|No| N[Increment retry_count\nStore last_error]
    N --> O{retry_count < 3?}
    O -->|Yes| P[Reschedule with\nExponential Backoff]
    O -->|No| Q[UPDATE status=FAILED\nAlert Admin]
    P --> C
    M --> R[Log Audit Trail]
    Q --> R
    R --> S[End]
    I --> S
```

## 10. Security Boundaries

```mermaid
graph TB
    subgraph "Public Internet"
        U[User Browser]
        QR[QR Scanner]
    end
    
    subgraph "DMZ / Edge"
        NGINX[Nginx Reverse Proxy\nTLS Termination\nRate Limiting\nWAF Rules]
    end
    
    subgraph "Internal Network"
        FE[Frontend Static Files]
        API[.NET 8 API\nJWT Validation\nAuthorization Policies]
        WORKER[Background Worker\nInternal Only]
    end
    
    subgraph "Data Layer"
        DB[(PostgreSQL 16\nRow-Level Security\nAudit Triggers\nEncrypted at Rest)]
    end
    
    subgraph "Identity Provider"
        ENTRA[Microsoft Entra ID\nOIDC/OAuth 2.0\nConditional Access\nMFA]
    end
    
    subgraph "External Services"
        EMAIL[Email Service\nTLS]
        BACKUP[Backup Storage\nEncrypted]
        PBI[Power BI\nService Principal]
    end
    
    U -->|HTTPS| NGINX
    QR -->|HTTPS| NGINX
    NGINX -->|Static| FE
    NGINX -->|Proxy /api| API
    API -->|OIDC| ENTRA
    API -->|SQL| DB
    WORKER -->|SQL| DB
    WORKER -->|SMTP/API| EMAIL
    DB -->|Backup| BACKUP
    DB -->|Direct Query| PBI
    
    style NGINX fill:#ffd800,color:#0e0e0e
    style API fill:#0e0e0e,color:#ffd800
    style DB fill:#2a2a2a,color:#fff
    style ENTRA fill:#f6f0cb,color:#0e0e0e
```

## 11. State Machine - Reservation Lifecycle

```mermaid
stateDiagram-v2
    [*] --> CONFIRMED : Create Reservation
    
    CONFIRMED --> CHECKED_IN : QR Check-in (Offices)
    CONFIRMED --> CANCELLED : User Cancel
    CONFIRMED --> NOT_CHECKED_IN : No-show (After end_time)
    CONFIRMED --> COMPLETED : End time passed (Meeting Rooms)
    
    CHECKED_IN --> CHECKED_OUT : QR Check-out / Auto at end_time
    CHECKED_IN --> CANCELLED : Support Cancel (with reason)
    
    CHECKED_OUT --> COMPLETED : Normal completion
    
    NOT_CHECKED_IN --> [*] : Marked as no-show
    COMPLETED --> [*] : Archived
    CANCELLED --> [*] : Archived
    REJECTED --> [*] : Admin rejection
    
    note right of CONFIRMED
        Active reservation
        Counts toward 5-future limit
        Modifiable by owner
    end note
    
    note right of CHECKED_IN
        Physical presence confirmed
        Only for OPEN_WORKSPACE/CLOSED_OFFICE
        Auto-checkout at end_time
    end note
    
    note right of COMPLETED
        Successful reservation
        Available for analytics
    end note
```

## 12. API Gateway / Routing

```mermaid
flowchart LR
    subgraph "Nginx Routes"
        A[/] --> FE[Frontend SPA]
        B[/api/v1/*] --> API[API Controllers]
        C[/health*] --> HC[Health Checks]
        D[/check-in/*] --> FE
        E[/assets/*] --> FE[Static Assets]
        F[/*.well-known/*] --> ENTRA[Entra ID Discovery]
    end
    
    subgraph "API Controller Routes"
        API --> R1[ResourcesController]
        API --> R2[ReservationsController]
        API --> R3[CheckInController]
        API --> R4[AdminController]
        API --> R5[AuthController]
    end
    
    subgraph "Authorization Policies"
        R1 --> P1[RequireAuthenticatedUser]
        R2 --> P2[RequireReservationAccess]
        R3 --> P3[RequireCheckInPermission]
        R4 --> P4[RequireAdminRole]
        R5 --> P5[AllowAnonymous]
    end
    
    style NGINX fill:#ffd800,color:#0e0e0e
```

---

*All diagrams use Mermaid.js syntax. Render in GitHub, VS Code, or any Mermaid-compatible viewer.*