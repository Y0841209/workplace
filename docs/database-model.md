# Modelo Lógico de Datos - Workplace Booking Platform

## 1. Entidades

| Entidad | Descripción | Tabla Física |
|---------|-------------|--------------|
| **AppSettings** | Configuración global singleton (límites, recordatorios, reglas) | `app_settings` |
| **Location** | Sede física (ciudad, país, zona horaria) | `locations` |
| **Floor** | Piso dentro de una sede | `floors` |
| **Zone** | Agrupación lógica de recursos en un piso | `zones` |
| **ResourceType** | Tipo de recurso: OPEN_WORKSPACE, CLOSED_OFFICE, MEETING_ROOM | `resource_types` |
| **Resource** | Espacio reservable (oficina/sala) con capacidad, QR, piso, zona | `resources` |
| **AppUser** | Usuario autenticado vinculado a Microsoft Entra ID | `app_users` |
| **BusinessProfile** | Perfil funcional de negocio: COLLABORATOR, ASSOCIATE, LEADER, DIRECTOR, PARTNER | `business_profiles` |
| **ApplicationRole** | Rol administrativo: USER, ROOM_ADMIN, SUPPORT, GLOBAL_ADMIN | `application_roles` |
| **UserBusinessProfile** | Asignación de perfil de negocio a usuario (con vigencia) | `user_business_profiles` |
| **UserApplicationRole** | Asignación de rol administrativo a usuario (con vigencia) | `user_application_roles` |
| **ResourceAccessPolicy** | Política de acceso: perfil × tipo recurso → permisos (ver, reservar, modificar propio) | `resource_access_policies` |
| **ReservationException** | Excepción temporal al límite de reservas futuras por usuario/tipo recurso | `reservation_exceptions` |
| **Reservation** | Reserva de un recurso por usuario en fecha/horario con estado | `reservations` |
| **CheckIn** | Confirmación de presencia via QR (solo oficinas) | `checkins` |
| **NotificationOutbox** | Cola transaccional de correos pendientes/enviados/fallidos | `notification_outbox` |
| **AuditLog** | Registro inmutable de eventos sensibles (actor, acción, entidad, antes/después) | `audit_logs` |

---

## 2. Relaciones

| Entidad Origen | Entidad Destino | Tipo | Descripción |
|----------------|-----------------|------|-------------|
| Location | Floor | 1:N | Una sede tiene múltiples pisos |
| Floor | Zone | 1:N | Un piso tiene múltiples zonas |
| Floor | Resource | 1:N | Un piso aloja múltiples recursos |
| Zone | Resource | 1:N | Una zona agrupa múltiples recursos (opcional) |
| ResourceType | Resource | 1:N | Un tipo clasifica múltiples recursos |
| ResourceType | ResourceAccessPolicy | 1:N | Un tipo tiene políticas por perfil |
| BusinessProfile | ResourceAccessPolicy | 1:N | Un perfil tiene políticas por tipo recurso |
| ResourceType | ReservationException | 1:N | Una excepción aplica a un tipo recurso (opcional) |
| AppUser | UserBusinessProfile | 1:N | Un usuario tiene múltiples perfiles (histórico) |
| AppUser | UserApplicationRole | 1:N | Un usuario tiene múltiples roles (histórico) |
| AppUser | Reservation | 1:N | Un usuario crea/reserva múltiples reservas |
| AppUser | CheckIn | 1:N | Un usuario realiza múltiples check-ins |
| AppUser | NotificationOutbox | 1:N | Un usuario recibe múltiples notificaciones |
| AppUser | AuditLog | 1:N | Un usuario ejecuta múltiples acciones auditables |
| Resource | Reservation | 1:N | Un recurso tiene múltiples reservas |
| Resource | CheckIn | 1:N | Un recurso recibe múltiples check-ins |
| Reservation | CheckIn | 1:1 | Una reserva genera máximo un check-in |
| Reservation | NotificationOutbox | 1:N | Una reserva dispara múltiples notificaciones |

---

## 3. Llaves Primarias

| Entidad | PK | Tipo | Generación |
|---------|----|------|------------|
| AppSettings | `id` | UUID | `gen_random_uuid()` (singleton) |
| Location | `id` | UUID | `gen_random_uuid()` |
| Floor | `id` | UUID | `gen_random_uuid()` |
| Zone | `id` | UUID | `gen_random_uuid()` |
| ResourceType | `code` | TEXT | Natural (código fijo) |
| Resource | `id` | UUID | `gen_random_uuid()` |
| AppUser | `id` | UUID | `gen_random_uuid()` |
| BusinessProfile | `code` | TEXT | Natural (código fijo) |
| ApplicationRole | `code` | TEXT | Natural (código fijo) |
| UserBusinessProfile | `id` | UUID | `gen_random_uuid()` |
| UserApplicationRole | `id` | UUID | `gen_random_uuid()` |
| ResourceAccessPolicy | `id` | UUID | `gen_random_uuid()` |
| ReservationException | `id` | UUID | `gen_random_uuid()` |
| Reservation | `id` | UUID | `gen_random_uuid()` |
| CheckIn | `id` | UUID | `gen_random_uuid()` |
| NotificationOutbox | `id` | UUID | `gen_random_uuid()` |
| AuditLog | `id` | UUID | `gen_random_uuid()` |

---

## 4. Llaves Foráneas

| Tabla Hija | Columna FK | Tabla Padre | Columna Padre | On Delete |
|------------|------------|-------------|---------------|-----------|
| `floors` | `location_id` | `locations` | `id` | RESTRICT |
| `zones` | `floor_id` | `floors` | `id` | RESTRICT |
| `resources` | `location_id` | `locations` | `id` | RESTRICT |
| `resources` | `floor_id` | `floors` | `id` | RESTRICT |
| `resources` | `zone_id` | `zones` | `id` | SET NULL |
| `resources` | `resource_type_code` | `resource_types` | `code` | RESTRICT |
| `user_business_profiles` | `user_id` | `app_users` | `id` | CASCADE |
| `user_business_profiles` | `profile_code` | `business_profiles` | `code` | RESTRICT |
| `user_business_profiles` | `assigned_by_user_id` | `app_users` | `id` | SET NULL |
| `user_application_roles` | `user_id` | `app_users` | `id` | CASCADE |
| `user_application_roles` | `role_code` | `application_roles` | `code` | RESTRICT |
| `user_application_roles` | `assigned_by_user_id` | `app_users` | `id` | SET NULL |
| `resource_access_policies` | `resource_type_code` | `resource_types` | `code` | RESTRICT |
| `resource_access_policies` | `business_profile_code` | `business_profiles` | `code` | RESTRICT |
| `reservation_exceptions` | `user_id` | `app_users` | `id` | CASCADE |
| `reservation_exceptions` | `applies_to_resource_type_code` | `resource_types` | `code` | SET NULL |
| `reservation_exceptions` | `created_by_user_id` | `app_users` | `id` | RESTRICT |
| `reservations` | `resource_id` | `resources` | `id` | RESTRICT |
| `reservations` | `user_id` | `app_users` | `id` | RESTRICT |
| `reservations` | `created_by_user_id` | `app_users` | `id` | RESTRICT |
| `reservations` | `cancelled_by_user_id` | `app_users` | `id` | SET NULL |
| `checkins` | `reservation_id` | `reservations` | `id` | CASCADE |
| `checkins` | `resource_id` | `resources` | `id` | RESTRICT |
| `checkins` | `user_id` | `app_users` | `id` | RESTRICT |
| `notification_outbox` | `reservation_id` | `reservations` | `id` | SET NULL |
| `notification_outbox` | `recipient_user_id` | `app_users` | `id` | RESTRICT |
| `audit_logs` | `actor_user_id` | `app_users` | `id` | SET NULL |

---

## 5. Cardinalidades

| Relación | Cardinalidad Origen | Cardinalidad Destino | Notas |
|----------|---------------------|----------------------|-------|
| Location → Floor | 1 | N | Un piso pertenece a una sede |
| Floor → Zone | 1 | N | Una zona pertenece a un piso |
| Floor → Resource | 1 | N | Un recurso está en un piso |
| Zone → Resource | 1 | N | Un recurso puede estar en una zona (opcional) |
| ResourceType → Resource | 1 | N | Un recurso tiene un tipo |
| ResourceType → ResourceAccessPolicy | 1 | N | Políticas por tipo recurso |
| BusinessProfile → ResourceAccessPolicy | 1 | N | Políticas por perfil |
| BusinessProfile → UserBusinessProfile | 1 | N | Asignación a usuarios |
| ApplicationRole → UserApplicationRole | 1 | N | Asignación a usuarios |
| AppUser → UserBusinessProfile | 1 | N | Historial de perfiles |
| AppUser → UserApplicationRole | 1 | N | Historial de roles |
| AppUser → Reservation (owner) | 1 | N | Usuario dueño de la reserva |
| AppUser → Reservation (creator) | 1 | N | Usuario que creó la reserva |
| AppUser → CheckIn | 1 | N | Usuario que hace check-in |
| AppUser → NotificationOutbox | 1 | N | Destinatario |
| AppUser → AuditLog | 1 | N | Actor de la acción |
| Resource → Reservation | 1 | N | Recurso reservado |
| Resource → CheckIn | 1 | N | Recurso donde se hace check-in |
| ResourceType → ReservationException | 1 | N | Excepción por tipo (opcional) |
| AppUser → ReservationException | 1 | N | Usuario con excepción |
| Reservation → CheckIn | 1 | 0..1 | Una reserva → cero o un check-in |
| Reservation → NotificationOutbox | 1 | N | Notificaciones derivadas |

---

## 6. Índices Recomendados

| Tabla | Índice | Columnas | Tipo | Justificación |
|-------|--------|----------|------|---------------|
| `app_settings` | `ux_app_settings_singleton` | `((true))` | UNIQUE | Garantiza singleton |
| `locations` | `ux_locations_code` | `code` | UNIQUE | Código único de sede |
| `floors` | `ux_floor_location_number` | `location_id, floor_number` | UNIQUE | Un piso por número por sede |
| `floors` | `ux_floor_location_code` | `location_id, code` | UNIQUE | Código único por sede |
| `zones` | `ux_zone_floor_code` | `floor_id, code` | UNIQUE | Código único por piso |
| `resources` | `ux_resources_code` | `code` | UNIQUE | Código único de recurso |
| `resources` | `ix_resources_type` | `resource_type_code` | BTREE | Filtro por tipo |
| `resources` | `ix_resources_floor` | `floor_id` | BTREE | Filtro por piso |
| `resources` | `ix_resources_active_reservable` | `active, reservable` | BTREE | Listar disponibles |
| `resources` | `ix_resources_public_qr` | `public_qr_id` | UNIQUE | Resolución QR rápida |
| `app_users` | `ux_app_users_entra` | `entra_object_id` | UNIQUE | Lookup por Entra ID |
| `app_users` | `ux_app_users_email` | `email` | UNIQUE | Login / notificaciones |
| `user_business_profiles` | `ux_user_profile_active` | `user_id, profile_code` WHERE active | UNIQUE PARTIAL | Perfil activo único |
| `user_application_roles` | `ux_user_role_active` | `user_id, role_code` WHERE active | UNIQUE PARTIAL | Rol activo único |
| `resource_access_policies` | `ux_resource_access_policy` | `resource_type_code, business_profile_code` | UNIQUE | Política única por combinación |
| `reservations` | `ix_reservations_user_date` | `user_id, reservation_date` | BTREE | Mis reservas por fecha |
| `reservations` | `ix_reservations_resource_date` | `resource_id, reservation_date` | BTREE | Disponibilidad por recurso/fecha |
| `reservations` | `ix_reservations_status` | `status` | BTREE | Filtro por estado |
| `reservations` | `ix_reservations_future_active` | `user_id, reservation_date, status` WHERE active | PARTIAL | Límite 5 reservas futuras |
| `checkins` | `ix_checkins_user` | `user_id, checked_in_at` | BTREE | Historial check-ins usuario |
| `checkins` | `ix_checkins_resource` | `resource_id, checked_in_at` | BTREE | Ocupación por recurso |
| `notification_outbox` | `ix_notification_pending` | `status, scheduled_at` WHERE pending | PARTIAL | Worker procesa pendientes |
| `audit_logs` | `ix_audit_logs_actor` | `actor_user_id, created_at DESC` | BTREE | Auditoría por actor |
| `audit_logs` | `ix_audit_logs_entity` | `entity_name, entity_id` | BTREE | Auditoría por entidad |
| `audit_logs` | `ix_audit_logs_action` | `action, created_at DESC` | BTREE | Auditoría por acción |
| `audit_logs` | `ix_audit_logs_correlation` | `correlation_id` | BTREE | Trazabilidad distribuida |

---

## 7. Constraints

### Check Constraints

| Tabla | Constraint | Condición | Descripción |
|-------|------------|-----------|-------------|
| `app_settings` | `ck_app_settings_limit` | `maximum_future_active_reservations > 0` | Límite positivo |
| `app_settings` | `ck_min_duration` | `minimum_duration_minutes >= 60` | Mínimo 1 hora |
| `app_settings` | `ck_reminder_minutes` | `reminder_minutes_before >= 0` | Recordatorio no negativo |
| `floors` | `floor_number > 0` | Número de piso positivo |
| `resources` | `ck_resource_capacity` | `capacity > 0` | Capacidad positiva |
| `resources` | `ck_resource_qr_policy` | `(type IN ('OPEN_WORKSPACE','CLOSED_OFFICE') AND public_qr_id IS NOT NULL) OR (type = 'MEETING_ROOM' AND public_qr_id IS NULL)` | QR solo en oficinas |
| `user_business_profiles` | `ck_profile_dates` | `expires_at IS NULL OR expires_at >= valid_from` | Fechas coherentes |
| `user_application_roles` | `ck_role_dates` | `expires_at IS NULL OR expires_at >= valid_from` | Fechas coherentes |
| `reservation_exceptions` | `ck_exception_limit` | `maximum_future_active_reservations > 0` | Límite positivo |
| `reservation_exceptions` | `ck_exception_dates` | `expires_at >= valid_from` | Fechas coherentes |
| `reservations` | `ck_reservation_time_order` | `end_time > start_time` | Fin después de inicio |
| `reservations` | `ck_reservation_min_duration` | `duration >= 1 hour` | Mínimo 1 hora |
| `reservations` | `ck_reservation_latest_end_time` | `end_time <= '23:59'` | Máximo 23:59 |
| `reservations` | `ck_attendee_count` | `attendee_count IS NULL OR attendee_count > 0` | Asistentes positivos |
| `notification_outbox` | `ck_notification_retry` | `retry_count >= 0` | Reintentos no negativos |

### Exclusion Constraints (Prevención Doble Reserva)

| Tabla | Constraint | Definición |
|-------|------------|------------|
| `reservations` | `ex_no_resource_overlap` | `EXCLUDE USING gist (resource_id WITH =, tsrange(reservation_date + start_time, reservation_date + end_time, '[)') WITH &&) WHERE (status IN ('CONFIRMED', 'CHECKED_IN'))` |
| `reservations` | `ex_no_user_overlap` | `EXCLUDE USING gist (user_id WITH =, tsrange(reservation_date + start_time, reservation_date + end_time, '[)') WITH &&) WHERE (status IN ('CONFIRMED', 'CHECKED_IN'))` |

### Unique Constraints Compuestas

| Tabla | Constraint | Columnas | Condición |
|-------|------------|----------|-----------|
| `user_business_profiles` | `ux_user_profile_active` | `user_id, profile_code` | `WHERE active = true` |
| `user_application_roles` | `ux_user_role_active` | `user_id, role_code` | `WHERE active = true` |
| `resource_access_policies` | `ux_resource_access_policy` | `resource_type_code, business_profile_code` | Siempre |

---

## 8. Diagrama Mermaid ER

```mermaid
erDiagram
    %% Configuración
    AppSettings ||--|| AppSettings : singleton

    %% Ubicación
    Location ||--o{ Floor : "1:N"
    Floor ||--o{ Zone : "1:N"
    Floor ||--o{ Resource : "1:N"
    Zone ||--o{ Resource : "1:N (opcional)"

    %% Tipos y Políticas
    ResourceType ||--o{ Resource : "1:N"
    ResourceType ||--o{ ResourceAccessPolicy : "1:N"
    ResourceType ||--o{ ReservationException : "1:N (opcional)"

    BusinessProfile ||--o{ ResourceAccessPolicy : "1:N"
    BusinessProfile ||--o{ UserBusinessProfile : "1:N"

    ApplicationRole ||--o{ UserApplicationRole : "1:N"

    %% Usuarios
    AppUser ||--o{ UserBusinessProfile : "1:N"
    AppUser ||--o{ UserApplicationRole : "1:N"
    AppUser ||--o{ Reservation : "crea/posee"
    AppUser ||--o{ CheckIn : "1:N"
    AppUser ||--o{ NotificationOutbox : "1:N"
    AppUser ||--o{ AuditLog : "1:N"
    AppUser ||--o{ ReservationException : "1:N"

    %% Reservas
    Resource ||--o{ Reservation : "1:N"
    Resource ||--o{ CheckIn : "1:N"
    Reservation ||--|| CheckIn : "1:0..1"
    Reservation ||--o{ NotificationOutbox : "1:N"

    %% Detalle de entidades
    AppSettings {
        uuid id PK
        int maximum_future_active_reservations
        int maximum_advance_days
        int minimum_duration_minutes
        time latest_end_time
        int reminder_minutes_before
        bool allow_cross_day_booking
        bool show_occupant_name_to_users
    }

    Location {
        uuid id PK
        string code UK
        string name
        string city
        string country
        string timezone
        bool active
    }

    Floor {
        uuid id PK
        uuid location_id FK
        int floor_number
        string code
        string name
        bool active
    }

    Zone {
        uuid id PK
        uuid floor_id FK
        string code
        string name
        bool active
    }

    ResourceType {
        string code PK
        string name
        bool qr_required
        bool checkin_required
        bool active
    }

    Resource {
        uuid id PK
        string code UK
        string name
        string resource_type_code FK
        uuid location_id FK
        uuid floor_id FK
        uuid zone_id FK NULL
        int capacity
        uuid public_qr_id UK NULL
        int qr_version
        bool active
        bool reservable
    }

    AppUser {
        uuid id PK
        uuid entra_object_id UK
        citext email UK
        string display_name
        string job_title
        string department
        bool active
        timestamptz last_login_at
    }

    BusinessProfile {
        string code PK
        string name
        bool active
    }

    ApplicationRole {
        string code PK
        string name
        string description
        bool active
    }

    UserBusinessProfile {
        uuid id PK
        uuid user_id FK
        string profile_code FK
        date valid_from
        date expires_at NULL
        bool active
        uuid assigned_by_user_id FK
        string assignment_reason
    }

    UserApplicationRole {
        uuid id PK
        uuid user_id FK
        string role_code FK
        date valid_from
        date expires_at NULL
        bool active
        uuid assigned_by_user_id FK
        string assignment_reason
    }

    ResourceAccessPolicy {
        uuid id PK
        string resource_type_code FK
        string business_profile_code FK
        bool can_view
        bool can_reserve
        bool can_modify_own
        bool active
    }

    ReservationException {
        uuid id PK
        uuid user_id FK
        int maximum_future_active_reservations
        string applies_to_resource_type_code FK NULL
        date valid_from
        date expires_at
        string reason
        bool active
        uuid created_by_user_id FK
    }

    Reservation {
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

    CheckIn {
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

    NotificationOutbox {
        uuid id PK
        uuid reservation_id FK NULL
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

    AuditLog {
        uuid id PK
        uuid actor_user_id FK NULL
        string action
        string entity_name
        uuid entity_id NULL
        jsonb before_value
        jsonb after_value
        string reason
        inet ip_address
        string user_agent
        uuid correlation_id
        timestamptz created_at
    }
```

---

## Notas de Implementación

1. **Esquema**: Todas las tablas residen en esquema `booking`
2. **Extensiones requeridas**: `pgcrypto` (UUIDs), `btree_gist` (exclusion constraints), `citext` (email case-insensitive)
3. **Triggers automáticos**: `set_updated_at()` en todas las tablas con `updated_at`
4. **Funciones helper**: `user_has_active_role()`, `user_can_reserve_resource()` para validaciones en BD
5. **Datos semilla**: Incluir en migración inicial - resource_types, business_profiles, application_roles, resource_access_policies, location/pisos/zonas, recursos 91 unidades
6. **Particionamiento futuro**: `audit_logs` y `notification_outbox` candidatos a partición por mes/año
7. **Soft delete**: Columna `active` + índices parciales `WHERE active = true` en lugar de borrado físico