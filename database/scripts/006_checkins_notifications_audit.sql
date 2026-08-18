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