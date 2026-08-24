-- ============================================================================
-- WORKPLACE BOOKING PLATFORM - BOOTSTRAP COMPLETO (una sola ejecucion)
-- ============================================================================
-- ATENCION: este script BORRA el esquema "booking" (DROP SCHEMA CASCADE) y lo
-- recrea desde cero. NO lo ejecutes si hay datos que quieras conservar.
--
-- Uso:
--   - Neon: SQL Editor -> pegar TODO el contenido -> Run
--   - psql : psql "postgresql://USER:PASS@HOST/DB?sslmode=require" -f database/bootstrap_full.sql
--
-- Ejecuta en orden: extensiones+esquema (001), catalogos (002), usuarios/
-- perfiles/politicas (003), sedes/pisos/zonas/recursos (004), reservas (005),
-- check-ins/auditoria (006), funciones/triggers (007), seed (008) y el
-- usuario de desarrollo (dev-seed corregido).
-- ============================================================================

-- =============================================
-- 0. LIMPIEZA TOTAL (idempotente: se puede re-ejecutar)
-- =============================================
DROP SCHEMA IF EXISTS booking CASCADE;
CREATE SCHEMA IF NOT EXISTS booking;
SET search_path TO booking, public;

-- ==================================================
-- 001_extensions_schema.sql
-- ==================================================

-- 001_extensions_schema.sql
-- Extensiones PostgreSQL requeridas y creación del esquema booking
-- Requiere permisos de superusuario (usuario POSTGRES_USER del contenedor,
-- o en Render: los planes permiten pgcrypto/btree_gist/citext).

CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "btree_gist";
CREATE EXTENSION IF NOT EXISTS "citext";

-- Esquema usado por EF Core (HasDefaultSchema("booking")) y por los scripts 003-009
CREATE SCHEMA IF NOT EXISTS booking;


-- ==================================================
-- 002_catalogs.sql
-- ==================================================

-- 002_catalogs.sql
-- Catálogos base: resource_types, business_profiles, application_roles
-- OJO: estos catálogos eran referenciados por FK en 003-004 e insertados por 008,
-- pero nunca se creaban en ningún script (002 estaba vacío). Esta es la corrección.
-- El DDL coincide con las configuraciones EF Core:
--   ResourceTypeConfiguration / BusinessProfileConfiguration / ApplicationRoleConfiguration
-- (PK = code, id UUID con default para compatibilidad con el modelo EF, active default true)

SET search_path TO booking, public;

-- Tipos de recurso: OPEN_WORKSPACE, CLOSED_OFFICE, MEETING_ROOM
CREATE TABLE resource_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    qr_required BOOLEAN NOT NULL DEFAULT FALSE,
    checkin_required BOOLEAN NOT NULL DEFAULT FALSE,
    active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Perfiles de negocio: COLLABORATOR, ASSOCIATE, LEADER, DIRECTOR, PARTNER
CREATE TABLE business_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Roles de aplicación: USER, ROOM_ADMIN, SUPPORT, GLOBAL_ADMIN
CREATE TABLE application_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    description TEXT,
    active BOOLEAN NOT NULL DEFAULT TRUE
);


-- ==================================================
-- 003_users_roles_profiles.sql
-- ==================================================

-- 003_users_roles_profiles.sql
-- Usuarios, asignación de perfiles y roles, políticas de acceso
-- Tablas: app_users, user_business_profiles, user_application_roles, resource_access_policies

SET search_path TO booking, public;

-- Usuarios vinculados a Microsoft Entra ID
CREATE TABLE app_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entra_object_id UUID NOT NULL UNIQUE,
    email CITEXT NOT NULL UNIQUE,
    display_name TEXT NOT NULL,
    job_title TEXT,
    department TEXT,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Asignación de perfiles de negocio a usuarios (histórico con vigencia)
CREATE TABLE user_business_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES app_users(id) ON DELETE CASCADE,
    profile_code TEXT NOT NULL REFERENCES business_profiles(code) ON DELETE RESTRICT,
    valid_from DATE NOT NULL DEFAULT CURRENT_DATE,
    expires_at DATE,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    assigned_by_user_id UUID REFERENCES app_users(id) ON DELETE SET NULL,
    assignment_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_profile_dates CHECK (expires_at IS NULL OR expires_at >= valid_from)
);

CREATE UNIQUE INDEX ux_user_profile_active
    ON user_business_profiles (user_id, profile_code)
    WHERE active = TRUE;

-- Asignación de roles administrativos a usuarios (histórico con vigencia)
CREATE TABLE user_application_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES app_users(id) ON DELETE CASCADE,
    role_code TEXT NOT NULL REFERENCES application_roles(code) ON DELETE RESTRICT,
    valid_from DATE NOT NULL DEFAULT CURRENT_DATE,
    expires_at DATE,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    assigned_by_user_id UUID REFERENCES app_users(id) ON DELETE SET NULL,
    assignment_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_role_dates CHECK (expires_at IS NULL OR expires_at >= valid_from)
);

CREATE UNIQUE INDEX ux_user_role_active
    ON user_application_roles (user_id, role_code)
    WHERE active = TRUE;

-- Políticas de acceso: perfil × tipo recurso → permisos
CREATE TABLE resource_access_policies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    resource_type_code TEXT NOT NULL REFERENCES resource_types(code) ON DELETE RESTRICT,
    business_profile_code TEXT NOT NULL REFERENCES business_profiles(code) ON DELETE RESTRICT,
    can_view BOOLEAN NOT NULL DEFAULT TRUE,
    can_reserve BOOLEAN NOT NULL DEFAULT FALSE,
    can_modify_own BOOLEAN NOT NULL DEFAULT TRUE,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ux_resource_access_policy UNIQUE (resource_type_code, business_profile_code)
);

-- ==================================================
-- 004_locations_resources.sql
-- ==================================================

-- 004_locations_resources.sql
-- Sedes, pisos, zonas, recursos
-- Tablas: locations, floors, zones, resources
-- Constraints: QR policy obligatorio, capacity > 0, code único

SET search_path TO booking, public;

-- Sedes
CREATE TABLE locations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    city TEXT NOT NULL DEFAULT 'Bogotá',
    country TEXT NOT NULL DEFAULT 'Colombia',
    timezone TEXT NOT NULL DEFAULT 'America/Bogota',
    active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Pisos
CREATE TABLE floors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    location_id UUID NOT NULL REFERENCES locations(id) ON DELETE RESTRICT,
    floor_number INTEGER NOT NULL,
    code TEXT NOT NULL,
    name TEXT NOT NULL,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ux_floor_location_number UNIQUE (location_id, floor_number),
    CONSTRAINT ux_floor_location_code UNIQUE (location_id, code)
);

-- Zonas
CREATE TABLE zones (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    floor_id UUID NOT NULL REFERENCES floors(id) ON DELETE RESTRICT,
    code TEXT NOT NULL,
    name TEXT NOT NULL,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ux_zone_floor_code UNIQUE (floor_id, code)
);

-- Recursos
CREATE TABLE resources (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    resource_type_code TEXT NOT NULL REFERENCES resource_types(code) ON DELETE RESTRICT,
    location_id UUID NOT NULL REFERENCES locations(id) ON DELETE RESTRICT,
    floor_id UUID NOT NULL REFERENCES floors(id) ON DELETE RESTRICT,
    zone_id UUID REFERENCES zones(id) ON DELETE SET NULL,
    capacity INTEGER NOT NULL DEFAULT 1,
    public_qr_id UUID UNIQUE,
    qr_version INTEGER NOT NULL DEFAULT 1,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    reservable BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_resource_capacity CHECK (capacity > 0),
    CONSTRAINT ck_resource_qr_policy CHECK (
        (resource_type_code IN ('OPEN_WORKSPACE', 'CLOSED_OFFICE') AND public_qr_id IS NOT NULL)
        OR (resource_type_code = 'MEETING_ROOM' AND public_qr_id IS NULL)
    )
);

CREATE INDEX ix_resources_type ON resources (resource_type_code);
CREATE INDEX ix_resources_floor ON resources (floor_id);
CREATE INDEX ix_resources_active_reservable ON resources (active, reservable);

-- ==================================================
-- 005_reservations.sql
-- ==================================================

-- 005_reservations.sql
-- Reservas con constraints de exclusión para evitar doble reserva
-- Tabla: reservations, app_settings (singleton)
-- Exclusion constraints GIST+tsrange: ex_no_resource_overlap, ex_no_user_overlap

SET search_path TO booking, public;

-- Tipos enumerados
CREATE TYPE reservation_status AS ENUM (
    'CONFIRMED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED',
    'COMPLETED', 'NOT_CHECKED_IN', 'REJECTED'
);

-- Configuración global (singleton)
CREATE TABLE app_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    maximum_future_active_reservations INTEGER NOT NULL DEFAULT 5,
    maximum_advance_days INTEGER,
    minimum_duration_minutes INTEGER NOT NULL DEFAULT 60,
    latest_end_time TIME NOT NULL DEFAULT '23:59',
    reminder_minutes_before INTEGER NOT NULL DEFAULT 15,
    allow_cross_day_booking BOOLEAN NOT NULL DEFAULT FALSE,
    show_occupant_name_to_users BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_app_settings_limit CHECK (maximum_future_active_reservations > 0),
    CONSTRAINT ck_min_duration CHECK (minimum_duration_minutes >= 60),
    CONSTRAINT ck_reminder_minutes CHECK (reminder_minutes_before >= 0)
);

CREATE UNIQUE INDEX ux_app_settings_singleton ON app_settings ((TRUE));

-- Reservas
CREATE TABLE reservations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    resource_id UUID NOT NULL REFERENCES resources(id) ON DELETE RESTRICT,
    user_id UUID NOT NULL REFERENCES app_users(id) ON DELETE RESTRICT,
    created_by_user_id UUID NOT NULL REFERENCES app_users(id) ON DELETE RESTRICT,
    reservation_date DATE NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    status reservation_status NOT NULL DEFAULT 'CONFIRMED',
    title TEXT,
    description TEXT,
    attendee_count INTEGER,
    support_change_reason TEXT,
    checked_in_at TIMESTAMPTZ,
    checked_out_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    cancelled_by_user_id UUID REFERENCES app_users(id) ON DELETE SET NULL,
    cancellation_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_reservation_time_order CHECK (end_time > start_time),
    CONSTRAINT ck_reservation_min_duration CHECK (
        (reservation_date + end_time) - (reservation_date + start_time) >= INTERVAL '1 hour'
    ),
    CONSTRAINT ck_reservation_latest_end_time CHECK (end_time <= TIME '23:59'),
    CONSTRAINT ck_attendee_count CHECK (attendee_count IS NULL OR attendee_count > 0)
);

-- Exclusion constraint: evitar doble reserva del mismo recurso
ALTER TABLE reservations DROP CONSTRAINT IF EXISTS ex_no_resource_overlap;
ALTER TABLE reservations ADD CONSTRAINT ex_no_resource_overlap
    EXCLUDE USING gist (
        resource_id WITH =,
        tsrange(reservation_date + start_time, reservation_date + end_time, '[)') WITH &&
    )
    WHERE (status IN ('CONFIRMED', 'CHECKED_IN'));

-- Exclusion constraint: evitar reservas superpuestas del mismo usuario
ALTER TABLE reservations DROP CONSTRAINT IF EXISTS ex_no_user_overlap;
ALTER TABLE reservations ADD CONSTRAINT ex_no_user_overlap
    EXCLUDE USING gist (
        user_id WITH =,
        tsrange(reservation_date + start_time, reservation_date + end_time, '[)') WITH &&
    )
    WHERE (status IN ('CONFIRMED', 'CHECKED_IN'));

-- Índices
CREATE INDEX ix_reservations_user_date ON reservations (user_id, reservation_date);
CREATE INDEX ix_reservations_resource_date ON reservations (resource_id, reservation_date);
CREATE INDEX ix_reservations_status ON reservations (status);
CREATE INDEX ix_reservations_future_active ON reservations (user_id, reservation_date, status)
    WHERE status IN ('CONFIRMED', 'CHECKED_IN');

-- ==================================================
-- 006_checkins_notifications_audit.sql
-- ==================================================

-- 006_checkins_notifications_audit.sql
-- Check-ins, notificaciones (outbox pattern), auditoría, excepciones de reserva
-- Tablas: checkins, notification_outbox, audit_logs, reservation_exceptions

SET search_path TO booking, public;

-- Tipos enumerados
CREATE TYPE notification_status AS ENUM ('PENDING', 'SENT', 'FAILED', 'CANCELLED');
CREATE TYPE notification_type AS ENUM (
    'RESERVATION_CREATED', 'RESERVATION_MODIFIED',
    'RESERVATION_CANCELLED', 'RESERVATION_REMINDER'
);
CREATE TYPE checkin_method AS ENUM ('QR');

-- Check-ins (1:1 con reservations)
CREATE TABLE checkins (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reservation_id UUID NOT NULL UNIQUE REFERENCES reservations(id) ON DELETE CASCADE,
    resource_id UUID NOT NULL REFERENCES resources(id) ON DELETE RESTRICT,
    user_id UUID NOT NULL REFERENCES app_users(id) ON DELETE RESTRICT,
    method checkin_method NOT NULL DEFAULT 'QR',
    scanned_public_qr_id UUID NOT NULL,
    checked_in_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_checkins_user ON checkins (user_id, checked_in_at);
CREATE INDEX ix_checkins_resource ON checkins (resource_id, checked_in_at);

-- Outbox de notificaciones (patrón transaccional)
CREATE TABLE notification_outbox (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reservation_id UUID REFERENCES reservations(id) ON DELETE SET NULL,
    recipient_user_id UUID NOT NULL REFERENCES app_users(id) ON DELETE RESTRICT,
    recipient_email CITEXT NOT NULL,
    type notification_type NOT NULL,
    subject TEXT NOT NULL,
    body TEXT NOT NULL,
    scheduled_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    sent_at TIMESTAMPTZ,
    status notification_status NOT NULL DEFAULT 'PENDING',
    retry_count INTEGER NOT NULL DEFAULT 0,
    last_error TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_notification_retry CHECK (retry_count >= 0)
);

CREATE INDEX ix_notification_pending ON notification_outbox (status, scheduled_at)
    WHERE status = 'PENDING';

-- Auditoría inmutable
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    actor_user_id UUID REFERENCES app_users(id) ON DELETE SET NULL,
    action TEXT NOT NULL,
    entity_name TEXT NOT NULL,
    entity_id UUID,
    before_value JSONB,
    after_value JSONB,
    reason TEXT,
    ip_address INET,
    user_agent TEXT,
    correlation_id UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_audit_logs_actor ON audit_logs (actor_user_id, created_at DESC);
CREATE INDEX ix_audit_logs_entity ON audit_logs (entity_name, entity_id);
CREATE INDEX ix_audit_logs_action ON audit_logs (action, created_at DESC);
CREATE INDEX ix_audit_logs_correlation ON audit_logs (correlation_id);

-- Excepciones temporales al límite de reservas futuras
CREATE TABLE reservation_exceptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES app_users(id) ON DELETE CASCADE,
    maximum_future_active_reservations INTEGER NOT NULL,
    applies_to_resource_type_code TEXT REFERENCES resource_types(code) ON DELETE SET NULL,
    valid_from DATE NOT NULL DEFAULT CURRENT_DATE,
    expires_at DATE NOT NULL,
    reason TEXT NOT NULL,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    created_by_user_id UUID NOT NULL REFERENCES app_users(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_exception_limit CHECK (maximum_future_active_reservations > 0),
    CONSTRAINT ck_exception_dates CHECK (expires_at >= valid_from)
);

-- ==================================================
-- 007_functions_triggers.sql
-- ==================================================

-- 007_functions_triggers.sql
-- Funciones helper y triggers básicos updated_at
-- Funciones: set_updated_at(), user_has_active_role(), user_can_reserve_resource()
-- Triggers: updated_at en tablas con columna updated_at

SET search_path TO booking, public;

-- Función genérica para actualizar updated_at
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$;

-- Triggers updated_at en tablas con columna updated_at
CREATE TRIGGER trg_app_settings_updated
    BEFORE UPDATE ON app_settings
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_locations_updated
    BEFORE UPDATE ON locations
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_floors_updated
    BEFORE UPDATE ON floors
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_zones_updated
    BEFORE UPDATE ON zones
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_resources_updated
    BEFORE UPDATE ON resources
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_app_users_updated
    BEFORE UPDATE ON app_users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_user_business_profiles_updated
    BEFORE UPDATE ON user_business_profiles
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_user_application_roles_updated
    BEFORE UPDATE ON user_application_roles
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_resource_access_policies_updated
    BEFORE UPDATE ON resource_access_policies
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_reservation_exceptions_updated
    BEFORE UPDATE ON reservation_exceptions
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_reservations_updated
    BEFORE UPDATE ON reservations
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_notification_outbox_updated
    BEFORE UPDATE ON notification_outbox
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Función: verificar si usuario tiene rol activo
CREATE OR REPLACE FUNCTION user_has_active_role(p_user_id UUID, p_role_code TEXT)
RETURNS BOOLEAN LANGUAGE SQL STABLE AS $$
    SELECT EXISTS (
        SELECT 1 FROM user_application_roles uar
        WHERE uar.user_id = p_user_id
          AND uar.role_code = p_role_code
          AND uar.active = TRUE
          AND CURRENT_DATE >= uar.valid_from
          AND (uar.expires_at IS NULL OR CURRENT_DATE <= uar.expires_at)
    );
$$;

-- Función: verificar si usuario puede reservar un tipo de recurso
CREATE OR REPLACE FUNCTION user_can_reserve_resource(p_user_id UUID, p_resource_type_code TEXT)
RETURNS BOOLEAN LANGUAGE SQL STABLE AS $$
    SELECT user_has_active_role(p_user_id, 'GLOBAL_ADMIN')
        OR EXISTS (
            SELECT 1
            FROM user_business_profiles ubp
            JOIN resource_access_policies rap
              ON rap.business_profile_code = ubp.profile_code
             AND rap.resource_type_code = p_resource_type_code
             AND rap.active = TRUE
             AND rap.can_reserve = TRUE
            WHERE ubp.user_id = p_user_id
              AND ubp.active = TRUE
              AND CURRENT_DATE >= ubp.valid_from
              AND (ubp.expires_at IS NULL OR CURRENT_DATE <= ubp.expires_at)
        );
$$;

-- Función: validar reglas de negocio de reserva
CREATE OR REPLACE FUNCTION validate_reservation_business_rules()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
    v_resource RECORD;
    v_max_reservations INTEGER;
    v_future_count INTEGER;
    v_has_exception BOOLEAN;
    v_resource_type TEXT;
BEGIN
    -- Solo validar en INSERT/UPDATE de reservas activas
    IF NEW.status NOT IN ('CONFIRMED', 'CHECKED_IN') THEN
        RETURN NEW;
    END IF;

    -- Obtener recurso
    SELECT * INTO v_resource
    FROM resources
    WHERE id = NEW.resource_id;

    IF v_resource IS NULL THEN
        RAISE EXCEPTION 'Recurso no encontrado: %', NEW.resource_id;
    END IF;

    -- Validar recurso activo y reservable
    IF NOT v_resource.active THEN
        RAISE EXCEPTION 'El recurso no está activo';
    END IF;
    IF NOT v_resource.reservable THEN
        RAISE EXCEPTION 'El recurso no es reservable';
    END IF;

    -- Validar duración mínima 1 hora (ya hay CHECK constraint, pero validamos aquí también)
    IF (NEW.reservation_date + NEW.end_time) - (NEW.reservation_date + NEW.start_time) < INTERVAL '1 hour' THEN
        RAISE EXCEPTION 'La reserva debe durar al menos 1 hora';
    END IF;

    -- Validar end_time máximo 23:59 (ya hay CHECK constraint)
    IF NEW.end_time > TIME '23:59' THEN
        RAISE EXCEPTION 'La hora máxima de finalización es 23:59';
    END IF;

    -- Validar attendee_count no supere capacity en MEETING_ROOM
    IF NEW.attendee_count IS NOT NULL AND v_resource.resource_type_code = 'MEETING_ROOM' THEN
        IF NEW.attendee_count > v_resource.capacity THEN
            RAISE EXCEPTION 'El número de asistentes (%) supera la capacidad de la sala (%)', NEW.attendee_count, v_resource.capacity;
        END IF;
    END IF;

    -- GLOBAL_ADMIN sin límites
    IF user_has_active_role(NEW.user_id, 'GLOBAL_ADMIN') THEN
        RETURN NEW;
    END IF;

    -- Obtener tipo de recurso
    v_resource_type := v_resource.resource_type_code;

    -- ROOM_ADMIN sin límite solo para MEETING_ROOM
    IF user_has_active_role(NEW.user_id, 'ROOM_ADMIN') AND v_resource_type = 'MEETING_ROOM' THEN
        RETURN NEW;
    END IF;

    -- Obtener límite de reservas futuras
    SELECT maximum_future_active_reservations INTO v_max_reservations
    FROM app_settings
    LIMIT 1;

    -- Contar reservas futuras activas del usuario
    SELECT COUNT(*) INTO v_future_count
    FROM reservations
    WHERE user_id = NEW.user_id
      AND status IN ('CONFIRMED', 'CHECKED_IN')
      AND reservation_date >= CURRENT_DATE
      AND id <> COALESCE(NEW.id, '00000000-0000-0000-0000-000000000000'::UUID);

    -- Verificar excepción vigente
    SELECT EXISTS (
        SELECT 1 FROM reservation_exceptions re
        WHERE re.user_id = NEW.user_id
          AND re.active = TRUE
          AND CURRENT_DATE >= re.valid_from
          AND CURRENT_DATE <= re.expires_at
          AND (re.applies_to_resource_type_code IS NULL OR re.applies_to_resource_type_code = v_resource_type)
    ) INTO v_has_exception;

    IF v_future_count >= v_max_reservations AND NOT v_has_exception THEN
        RAISE EXCEPTION 'Límite de % reservas futuras activas excedido', v_max_reservations;
    END IF;

    RETURN NEW;
END;
$$;

-- Trigger: validar reglas de negocio de reserva
DROP TRIGGER IF EXISTS trg_validate_reservation_business_rules ON reservations;
CREATE TRIGGER trg_validate_reservation_business_rules
    BEFORE INSERT OR UPDATE ON reservations
    FOR EACH ROW EXECUTE FUNCTION validate_reservation_business_rules();

-- Función: validar reglas de negocio de check-in
CREATE OR REPLACE FUNCTION validate_checkin_business_rules()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
    v_reservation RECORD;
    v_resource RECORD;
    v_reservation_start TIMESTAMPTZ;
    v_reservation_end TIMESTAMPTZ;
BEGIN
    -- Obtener reserva y recurso
    SELECT r.*, res.resource_type_code, res.public_qr_id, res.capacity
    INTO v_reservation
    FROM reservations r
    JOIN resources res ON res.id = r.resource_id
    WHERE r.id = NEW.reservation_id;

    IF v_reservation IS NULL THEN
        RAISE EXCEPTION 'Reserva no encontrada: %', NEW.reservation_id;
    END IF;

    -- Obtener recurso
    SELECT * INTO v_resource
    FROM resources
    WHERE id = v_reservation.resource_id;

    -- Check-in solo para OPEN_WORKSPACE y CLOSED_OFFICE
    IF v_resource.resource_type_code NOT IN ('OPEN_WORKSPACE', 'CLOSED_OFFICE') THEN
        RAISE EXCEPTION 'Check-in solo permitido para oficinas (OPEN_WORKSPACE, CLOSED_OFFICE)';
    END IF;

    -- MEETING_ROOM no permite check-in
    IF v_resource.resource_type_code = 'MEETING_ROOM' THEN
        RAISE EXCEPTION 'Las salas de juntas no permiten check-in';
    END IF;

    -- El QR escaneado debe coincidir con public_qr_id
    IF v_resource.public_qr_id <> NEW.scanned_public_qr_id THEN
        RAISE EXCEPTION 'QR no corresponde al recurso de la reserva';
    END IF;

    -- La reserva debe pertenecer al usuario autenticado
    IF v_reservation.user_id <> NEW.user_id THEN
        RAISE EXCEPTION 'La reserva no pertenece al usuario';
    END IF;

    -- La reserva debe estar en estado CONFIRMED
    IF v_reservation.status <> 'CONFIRMED' THEN
        RAISE EXCEPTION 'Solo reservas CONFIRMED permiten check-in (estado actual: %)', v_reservation.status;
    END IF;

    -- La reserva debe corresponder al día actual
    IF v_reservation.reservation_date <> CURRENT_DATE THEN
        RAISE EXCEPTION 'La reserva no corresponde al día actual';
    END IF;

    -- El check-in debe ocurrir dentro del horario reservado
    v_reservation_start := v_reservation.reservation_date + v_reservation.start_time;
    v_reservation_end := v_reservation.reservation_date + v_reservation.end_time;

    IF NOW() < v_reservation_start THEN
        RAISE EXCEPTION 'La reserva aún no ha comenzado (inicio: %)', v_reservation_start;
    END IF;

    IF NOW() > v_reservation_end THEN
        RAISE EXCEPTION 'La reserva ya ha finalizado (fin: %)', v_reservation_end;
    END IF;

    RETURN NEW;
END;
$$;

-- Trigger: validar reglas de negocio de check-in
DROP TRIGGER IF EXISTS trg_validate_checkin_business_rules ON checkins;
CREATE TRIGGER trg_validate_checkin_business_rules
    BEFORE INSERT ON checkins
    FOR EACH ROW EXECUTE FUNCTION validate_checkin_business_rules();

-- Función: marcar reserva como checked-in
CREATE OR REPLACE FUNCTION mark_reservation_checked_in()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    UPDATE reservations
    SET status = 'CHECKED_IN',
        checked_in_at = NEW.checked_in_at,
        updated_at = NOW()
    WHERE id = NEW.reservation_id;

    RETURN NEW;
END;
$$;

-- Trigger: marcar reserva como checked-in tras check-in exitoso
DROP TRIGGER IF EXISTS trg_mark_reservation_checked_in ON checkins;
CREATE TRIGGER trg_mark_reservation_checked_in
    AFTER INSERT ON checkins
    FOR EACH ROW EXECUTE FUNCTION mark_reservation_checked_in();

-- ==================================================
-- 008_seed_data.sql
-- ==================================================

-- 008_seed_data.sql
-- Datos semilla iniciales
-- Configuración global, catálogos base, sede Bogotá, pisos, zonas, recursos (91 unidades)

SET search_path TO booking, public;

-- =============================================
-- 1. APP_SETTINGS (singleton)
-- =============================================
INSERT INTO app_settings (
    maximum_future_active_reservations,
    maximum_advance_days,
    minimum_duration_minutes,
    latest_end_time,
    reminder_minutes_before,
    allow_cross_day_booking,
    show_occupant_name_to_users
) VALUES (
    5, NULL, 60, '23:59', 15, FALSE, TRUE
) ON CONFLICT DO NOTHING;

-- =============================================
-- 2. RESOURCE_TYPES
-- =============================================
INSERT INTO resource_types (code, name, qr_required, checkin_required) VALUES
    ('OPEN_WORKSPACE', 'Oficina abierta', TRUE, TRUE),
    ('CLOSED_OFFICE', 'Oficina cerrada', TRUE, TRUE),
    ('MEETING_ROOM', 'Sala de juntas', FALSE, FALSE)
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    qr_required = EXCLUDED.qr_required,
    checkin_required = EXCLUDED.checkin_required;

-- =============================================
-- 3. BUSINESS_PROFILES
-- =============================================
INSERT INTO business_profiles (code, name) VALUES
    ('COLLABORATOR', 'Colaborador'),
    ('ASSOCIATE', 'Asociado'),
    ('LEADER', 'Líder'),
    ('DIRECTOR', 'Director'),
    ('PARTNER', 'Socio')
ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name;

-- =============================================
-- 4. APPLICATION_ROLES
-- =============================================
INSERT INTO application_roles (code, name, description) VALUES
    ('USER', 'Usuario', 'Usuario estándar autenticado'),
    ('ROOM_ADMIN', 'Administrador de salas', 'Puede administrar y reservar salas de juntas sin límite futuro'),
    ('SUPPORT', 'Soporte TI', 'Puede modificar reservas y consultar auditoría'),
    ('GLOBAL_ADMIN', 'Administrador global', 'Control total de la aplicación')
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    description = EXCLUDED.description;

-- =============================================
-- 5. RESOURCE_ACCESS_POLICIES (15 combinaciones)
-- =============================================
INSERT INTO resource_access_policies (resource_type_code, business_profile_code, can_view, can_reserve, can_modify_own) VALUES
    -- OPEN_WORKSPACE: todos pueden ver y reservar
    ('OPEN_WORKSPACE', 'COLLABORATOR', TRUE, TRUE, TRUE),
    ('OPEN_WORKSPACE', 'ASSOCIATE',  TRUE, TRUE, TRUE),
    ('OPEN_WORKSPACE', 'LEADER',     TRUE, TRUE, TRUE),
    ('OPEN_WORKSPACE', 'DIRECTOR',   TRUE, TRUE, TRUE),
    ('OPEN_WORKSPACE', 'PARTNER',    TRUE, TRUE, TRUE),
    -- CLOSED_OFFICE: solo LEADER, DIRECTOR, PARTNER
    ('CLOSED_OFFICE', 'COLLABORATOR', TRUE, FALSE, FALSE),
    ('CLOSED_OFFICE', 'ASSOCIATE',   TRUE, FALSE, FALSE),
    ('CLOSED_OFFICE', 'LEADER',      TRUE, TRUE,  TRUE),
    ('CLOSED_OFFICE', 'DIRECTOR',    TRUE, TRUE,  TRUE),
    ('CLOSED_OFFICE', 'PARTNER',     TRUE, TRUE,  TRUE),
    -- MEETING_ROOM: todos pueden ver y reservar
    ('MEETING_ROOM', 'COLLABORATOR', TRUE, TRUE, TRUE),
    ('MEETING_ROOM', 'ASSOCIATE',    TRUE, TRUE, TRUE),
    ('MEETING_ROOM', 'LEADER',       TRUE, TRUE, TRUE),
    ('MEETING_ROOM', 'DIRECTOR',     TRUE, TRUE, TRUE),
    ('MEETING_ROOM', 'PARTNER',      TRUE, TRUE, TRUE)
ON CONFLICT (resource_type_code, business_profile_code) DO UPDATE SET
    can_view = EXCLUDED.can_view,
    can_reserve = EXCLUDED.can_reserve,
    can_modify_own = EXCLUDED.can_modify_own,
    active = TRUE;

-- =============================================
-- 6. LOCATION: SEDE-PRINCIPAL
-- =============================================
INSERT INTO locations (code, name, city, country, timezone) VALUES
    ('SEDE-PRINCIPAL', 'Sede principal', 'Bogotá', 'Colombia', 'America/Bogota')
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name, city = EXCLUDED.city, country = EXCLUDED.country, timezone = EXCLUDED.timezone;

-- =============================================
-- 7. FLOORS: 3, 6, 10
-- =============================================
INSERT INTO floors (location_id, floor_number, code, name)
SELECT l.id, f.floor_number, f.code, f.name
FROM locations l
CROSS JOIN (VALUES
    (3, 'P03', 'Piso 3'),
    (6, 'P06', 'Piso 6'),
    (10, 'P10', 'Piso 10')
) AS f(floor_number, code, name)
WHERE l.code = 'SEDE-PRINCIPAL'
ON CONFLICT (location_id, floor_number) DO UPDATE SET
    code = EXCLUDED.code, name = EXCLUDED.name;

-- =============================================
-- 8. ZONES: una zona general por piso
-- =============================================
INSERT INTO zones (floor_id, code, name)
SELECT id, code || '-GENERAL', 'Zona general ' || name
FROM floors
ON CONFLICT (floor_id, code) DO UPDATE SET name = EXCLUDED.name;

-- =============================================
-- 9. RESOURCES (91 unidades totales)
-- =============================================

-- ----- PISO 3 -----
-- 30 OPEN_WORKSPACE
INSERT INTO resources (code, name, resource_type_code, location_id, floor_id, zone_id, capacity, public_qr_id)
SELECT 'P03-OA-' || LPAD(gs::TEXT, 3, '0'),
       'Oficina abierta P03 ' || LPAD(gs::TEXT, 3, '0'),
       'OPEN_WORKSPACE', l.id, f.id, z.id, 1, gen_random_uuid()
FROM generate_series(1, 30) gs
JOIN locations l ON l.code = 'SEDE-PRINCIPAL'
JOIN floors f ON f.location_id = l.id AND f.floor_number = 3
JOIN zones z ON z.floor_id = f.id AND z.code = 'P03-GENERAL'
ON CONFLICT (code) DO NOTHING;

-- 9 CLOSED_OFFICE
INSERT INTO resources (code, name, resource_type_code, location_id, floor_id, zone_id, capacity, public_qr_id)
SELECT 'P03-OC-' || LPAD(gs::TEXT, 3, '0'),
       'Oficina cerrada P03 ' || LPAD(gs::TEXT, 3, '0'),
       'CLOSED_OFFICE', l.id, f.id, z.id, 1, gen_random_uuid()
FROM generate_series(1, 9) gs
JOIN locations l ON l.code = 'SEDE-PRINCIPAL'
JOIN floors f ON f.location_id = l.id AND f.floor_number = 3
JOIN zones z ON z.floor_id = f.id AND z.code = 'P03-GENERAL'
ON CONFLICT (code) DO NOTHING;

-- 2 MEETING_ROOM (SJ-06, SJ-07 capacidad 8)
INSERT INTO resources (code, name, resource_type_code, location_id, floor_id, zone_id, capacity, public_qr_id)
SELECT x.code, x.name, 'MEETING_ROOM', l.id, f.id, z.id, x.capacity, NULL
FROM (VALUES
    ('SJ-06', 'Sala de juntas 06', 8),
    ('SJ-07', 'Sala de juntas 07', 8)
) AS x(code, name, capacity)
JOIN locations l ON l.code = 'SEDE-PRINCIPAL'
JOIN floors f ON f.location_id = l.id AND f.floor_number = 3
JOIN zones z ON z.floor_id = f.id AND z.code = 'P03-GENERAL'
ON CONFLICT (code) DO NOTHING;

-- ----- PISO 6 -----
-- 18 OPEN_WORKSPACE
INSERT INTO resources (code, name, resource_type_code, location_id, floor_id, zone_id, capacity, public_qr_id)
SELECT 'P06-OA-' || LPAD(gs::TEXT, 3, '0'),
       'Oficina abierta P06 ' || LPAD(gs::TEXT, 3, '0'),
       'OPEN_WORKSPACE', l.id, f.id, z.id, 1, gen_random_uuid()
FROM generate_series(1, 18) gs
JOIN locations l ON l.code = 'SEDE-PRINCIPAL'
JOIN floors f ON f.location_id = l.id AND f.floor_number = 6
JOIN zones z ON z.floor_id = f.id AND z.code = 'P06-GENERAL'
ON CONFLICT (code) DO NOTHING;

-- 10 CLOSED_OFFICE
INSERT INTO resources (code, name, resource_type_code, location_id, floor_id, zone_id, capacity, public_qr_id)
SELECT 'P06-OC-' || LPAD(gs::TEXT, 3, '0'),
       'Oficina cerrada P06 ' || LPAD(gs::TEXT, 3, '0'),
       'CLOSED_OFFICE', l.id, f.id, z.id, 1, gen_random_uuid()
FROM generate_series(1, 10) gs
JOIN locations l ON l.code = 'SEDE-PRINCIPAL'
JOIN floors f ON f.location_id = l.id AND f.floor_number = 6
JOIN zones z ON z.floor_id = f.id AND z.code = 'P06-GENERAL'
ON CONFLICT (code) DO NOTHING;

-- 5 MEETING_ROOM (SJ-01 a SJ-05)
INSERT INTO resources (code, name, resource_type_code, location_id, floor_id, zone_id, capacity, public_qr_id)
SELECT x.code, x.name, 'MEETING_ROOM', l.id, f.id, z.id, x.capacity, NULL
FROM (VALUES
    ('SJ-01', 'Sala de juntas 01', 12),
    ('SJ-02', 'Sala de juntas 02', 12),
    ('SJ-03', 'Sala de juntas 03', 6),
    ('SJ-04', 'Sala de juntas 04', 5),
    ('SJ-05', 'Sala de juntas 05', 24)
) AS x(code, name, capacity)
JOIN locations l ON l.code = 'SEDE-PRINCIPAL'
JOIN floors f ON f.location_id = l.id AND f.floor_number = 6
JOIN zones z ON z.floor_id = f.id AND z.code = 'P06-GENERAL'
ON CONFLICT (code) DO NOTHING;

-- ----- PISO 10 -----
-- 12 OPEN_WORKSPACE
INSERT INTO resources (code, name, resource_type_code, location_id, floor_id, zone_id, capacity, public_qr_id)
SELECT 'P10-OA-' || LPAD(gs::TEXT, 3, '0'),
       'Oficina abierta P10 ' || LPAD(gs::TEXT, 3, '0'),
       'OPEN_WORKSPACE', l.id, f.id, z.id, 1, gen_random_uuid()
FROM generate_series(1, 12) gs
JOIN locations l ON l.code = 'SEDE-PRINCIPAL'
JOIN floors f ON f.location_id = l.id AND f.floor_number = 10
JOIN zones z ON z.floor_id = f.id AND z.code = 'P10-GENERAL'
ON CONFLICT (code) DO NOTHING;

-- 5 CLOSED_OFFICE
INSERT INTO resources (code, name, resource_type_code, location_id, floor_id, zone_id, capacity, public_qr_id)
SELECT 'P10-OC-' || LPAD(gs::TEXT, 3, '0'),
       'Oficina cerrada P10 ' || LPAD(gs::TEXT, 3, '0'),
       'CLOSED_OFFICE', l.id, f.id, z.id, 1, gen_random_uuid()
FROM generate_series(1, 5) gs
JOIN locations l ON l.code = 'SEDE-PRINCIPAL'
JOIN floors f ON f.location_id = l.id AND f.floor_number = 10
JOIN zones z ON z.floor_id = f.id AND z.code = 'P10-GENERAL'
ON CONFLICT (code) DO NOTHING;

-- =============================================
-- 10. USUARIO DE DESARROLLO (para probar con auth local)
-- El DevelopmentAuthenticationHandler emite claims con NameIdentifier/Sub =
-- 11111111-1111-1111-1111-111111111111 y email dev@local.com, por eso el
-- usuario debe existir con ese mismo id y entra_object_id.
-- =============================================
INSERT INTO app_users (id, entra_object_id, email, display_name, job_title, department, active)
VALUES (
    '11111111-1111-1111-1111-111111111111',
    '11111111-1111-1111-1111-111111111111',
    'dev@local.com',
    'Developer Local',
    'Software Engineer',
    'Engineering',
    TRUE
) ON CONFLICT (entra_object_id) DO NOTHING;

INSERT INTO user_application_roles (user_id, role_code, valid_from, active, assigned_by_user_id, assignment_reason)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'GLOBAL_ADMIN', CURRENT_DATE, TRUE, '11111111-1111-1111-1111-111111111111', 'Development admin access'),
    ('11111111-1111-1111-1111-111111111111', 'ROOM_ADMIN',   CURRENT_DATE, TRUE, '11111111-1111-1111-1111-111111111111', 'Development room admin'),
    ('11111111-1111-1111-1111-111111111111', 'SUPPORT',      CURRENT_DATE, TRUE, '11111111-1111-1111-1111-111111111111', 'Development support'),
    ('11111111-1111-1111-1111-111111111111', 'USER',         CURRENT_DATE, TRUE, '11111111-1111-1111-1111-111111111111', 'Development user')
ON CONFLICT DO NOTHING;

INSERT INTO user_business_profiles (user_id, profile_code, valid_from, active, assigned_by_user_id, assignment_reason)
VALUES ('11111111-1111-1111-1111-111111111111', 'LEADER', CURRENT_DATE, TRUE, '11111111-1111-1111-1111-111111111111', 'Development leader access')
ON CONFLICT DO NOTHING;

-- Reservas de prueba para hoy (recursos de oficina abierta y una sala)
INSERT INTO reservations (resource_id, user_id, created_by_user_id, reservation_date, start_time, end_time, status, title, description, attendee_count)
SELECT r.id, '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', CURRENT_DATE, '09:00', '11:00', 'CONFIRMED', 'Reserva de prueba', 'Reserva de desarrollo', 1
FROM resources r
WHERE r.resource_type_code = 'OPEN_WORKSPACE'
ORDER BY r.code
LIMIT 3
ON CONFLICT DO NOTHING;

INSERT INTO reservations (resource_id, user_id, created_by_user_id, reservation_date, start_time, end_time, status, title, description, attendee_count)
SELECT r.id, '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', CURRENT_DATE, '14:00', '15:00', 'CONFIRMED', 'Team meeting', 'Sincronizacion de equipo', 5
FROM resources r
WHERE r.code = 'SJ-01'
LIMIT 1
ON CONFLICT DO NOTHING;

-- =============================================
-- VERIFICACION FINAL
-- =============================================
SELECT 'resource_types' AS tabla, COUNT(*) AS filas FROM resource_types
UNION ALL SELECT 'business_profiles', COUNT(*) FROM business_profiles
UNION ALL SELECT 'application_roles', COUNT(*) FROM application_roles
UNION ALL SELECT 'resource_access_policies', COUNT(*) FROM resource_access_policies
UNION ALL SELECT 'resources', COUNT(*) FROM resources
UNION ALL SELECT 'app_users', COUNT(*) FROM app_users;
