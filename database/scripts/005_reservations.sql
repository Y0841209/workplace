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