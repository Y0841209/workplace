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