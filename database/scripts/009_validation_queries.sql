-- 009_validation_queries.sql
-- Consultas de validación post-despliegue
-- Verificación de integridad de datos y reglas de negocio

SET search_path TO booking, public;

-- =============================================
-- 1. TOTAL POR TIPO DE RECURSO
-- =============================================
SELECT '1. Total por tipo de recurso' AS validacion;
SELECT
    rt.code AS tipo_recurso,
    rt.name AS nombre,
    COUNT(r.id) AS total_recursos
FROM resource_types rt
LEFT JOIN resources r ON r.resource_type_code = rt.code AND r.active = TRUE
WHERE rt.active = TRUE
GROUP BY rt.code, rt.name
ORDER BY rt.code;

-- =============================================
-- 2. TOTAL POR PISO Y TIPO
-- =============================================
SELECT '2. Total por piso y tipo' AS validacion;
SELECT
    f.floor_number AS piso,
    r.resource_type_code AS tipo_recurso,
    COUNT(r.id) AS total_recursos
FROM resources r
JOIN floors f ON f.id = r.floor_id
WHERE r.active = TRUE AND r.reservable = TRUE
GROUP BY f.floor_number, r.resource_type_code
ORDER BY f.floor_number, r.resource_type_code;

-- =============================================
-- 3. OFICINAS SIN QR (OPEN_WORKSPACE y CLOSED_OFFICE sin public_qr_id)
-- =============================================
SELECT '3. Oficinas sin QR (ERROR si > 0)' AS validacion;
SELECT
    r.code,
    r.name,
    r.resource_type_code,
    r.public_qr_id
FROM resources r
WHERE r.active = TRUE
  AND r.resource_type_code IN ('OPEN_WORKSPACE', 'CLOSED_OFFICE')
  AND r.public_qr_id IS NULL;

-- =============================================
-- 4. SALAS CON QR POR ERROR (MEETING_ROOM con public_qr_id no NULL)
-- =============================================
SELECT '4. Salas con QR por error (ERROR si > 0)' AS validacion;
SELECT
    r.code,
    r.name,
    r.resource_type_code,
    r.public_qr_id
FROM resources r
WHERE r.active = TRUE
  AND r.resource_type_code = 'MEETING_ROOM'
  AND r.public_qr_id IS NOT NULL;

-- =============================================
-- 5. RECURSOS TOTALES
-- =============================================
SELECT '5. Recursos totales activos y reservables' AS validacion;
SELECT
    COUNT(*) AS total_recursos_activos_reservables
FROM resources
WHERE active = TRUE AND reservable = TRUE;

-- =============================================
-- 6. RESERVAS SUPERPUESTAS (verificar exclusion constraints funcionando)
-- =============================================
SELECT '6. Reservas superpuestas (ERROR si > 0)' AS validacion;
SELECT
    r1.id AS reserva_1,
    r2.id AS reserva_2,
    r1.resource_id,
    r1.user_id,
    r1.reservation_date,
    r1.start_time,
    r1.end_time,
    r2.start_time AS start_2,
    r2.end_time AS end_2
FROM reservations r1
JOIN reservations r2
  ON r1.resource_id = r2.resource_id
  AND r1.id < r2.id
  AND r1.reservation_date = r2.reservation_date
  AND r1.status IN ('CONFIRMED', 'CHECKED_IN')
  AND r2.status IN ('CONFIRMED', 'CHECKED_IN')
  AND r1.start_time < r2.end_time
  AND r2.start_time < r1.end_time;

-- También verificar superposición por usuario
SELECT
    r1.id AS reserva_1,
    r2.id AS reserva_2,
    r1.user_id,
    r1.reservation_date,
    r1.start_time,
    r1.end_time,
    r2.start_time AS start_2,
    r2.end_time AS end_2
FROM reservations r1
JOIN reservations r2
  ON r1.user_id = r2.user_id
  AND r1.id < r2.id
  AND r1.reservation_date = r2.reservation_date
  AND r1.status IN ('CONFIRMED', 'CHECKED_IN')
  AND r2.status IN ('CONFIRMED', 'CHECKED_IN')
  AND r1.start_time < r2.end_time
  AND r2.start_time < r1.end_time;

-- =============================================
-- 7. USUARIOS CON MÁS DE 5 RESERVAS FUTURAS ACTIVAS
-- =============================================
SELECT '7. Usuarios con >5 reservas futuras activas (ERROR si > 0)' AS validacion;
SELECT
    u.id AS user_id,
    u.email,
    u.display_name,
    COUNT(r.id) AS total_reservas_futuras
FROM app_users u
JOIN reservations r ON r.user_id = u.id
WHERE r.status IN ('CONFIRMED', 'CHECKED_IN')
  AND r.reservation_date >= CURRENT_DATE
  AND r.id NOT IN (
      SELECT id FROM reservations
      WHERE status IN ('CANCELLED', 'COMPLETED', 'NOT_CHECKED_IN', 'REJECTED')
  )
GROUP BY u.id, u.email, u.display_name
HAVING COUNT(r.id) > 5
ORDER BY total_reservas_futuras DESC;

-- =============================================
-- 8. ROOM_ADMIN CON RESERVAS DE SALAS
-- =============================================
SELECT '8. ROOM_ADMIN con reservas de salas (INFO)' AS validacion;
SELECT
    u.id AS user_id,
    u.email,
    u.display_name,
    r.id AS reserva_id,
    r.reservation_date,
    r.start_time,
    r.end_time,
    res.code AS recurso_code,
    res.name AS recurso_name,
    res.resource_type_code
FROM app_users u
JOIN user_application_roles uar ON uar.user_id = u.id
JOIN reservations r ON r.user_id = u.id
JOIN resources res ON res.id = r.resource_id
WHERE uar.role_code = 'ROOM_ADMIN'
  AND uar.active = TRUE
  AND CURRENT_DATE >= uar.valid_from
  AND (uar.expires_at IS NULL OR CURRENT_DATE <= uar.expires_at)
  AND r.status IN ('CONFIRMED', 'CHECKED_IN')
  AND res.resource_type_code = 'MEETING_ROOM'
ORDER BY u.email, r.reservation_date;

-- =============================================
-- 9. CHECK-INS EN MEETING_ROOM POR ERROR
-- =============================================
SELECT '9. Check-ins en MEETING_ROOM por error (ERROR si > 0)' AS validacion;
SELECT
    c.id AS checkin_id,
    c.reservation_id,
    c.user_id,
    c.checked_in_at,
    res.code AS recurso_code,
    res.name AS recurso_name,
    res.resource_type_code
FROM checkins c
JOIN reservations r ON r.id = c.reservation_id
JOIN resources res ON res.id = c.resource_id
WHERE res.resource_type_code = 'MEETING_ROOM';

-- =============================================
-- VALIDACIONES ADICIONALES
-- =============================================

-- Verificar unicidad de public_qr_id
SELECT 'Validación: Unicidad de public_qr_id' AS validacion;
SELECT
    public_qr_id,
    COUNT(*) AS count
FROM resources
WHERE public_qr_id IS NOT NULL
GROUP BY public_qr_id
HAVING COUNT(*) > 1;

-- Verificar capacity > 0
SELECT 'Validación: capacity > 0' AS validacion;
SELECT code, name, resource_type_code, capacity
FROM resources
WHERE capacity <= 0;

-- Verificar reservations sin resource_id válido
SELECT 'Validación: reservations huérfanas' AS validacion;
SELECT r.id
FROM reservations r
LEFT JOIN resources res ON res.id = r.resource_id
WHERE res.id IS NULL;

-- Verificar checkins sin reservation_id válido
SELECT 'Validación: checkins huérfanos' AS validacion;
SELECT c.id
FROM checkins c
LEFT JOIN reservations r ON r.id = c.reservation_id
WHERE r.id IS NULL;

-- Resumen final
SELECT 'RESUMEN FINAL' AS seccion,
       (SELECT COUNT(*) FROM resources WHERE active AND reservable AND resource_type_code = 'OPEN_WORKSPACE') AS open_workspace,
       (SELECT COUNT(*) FROM resources WHERE active AND reservable AND resource_type_code = 'CLOSED_OFFICE') AS closed_office,
       (SELECT COUNT(*) FROM resources WHERE active AND reservable AND resource_type_code = 'MEETING_ROOM') AS meeting_room,
       (SELECT COUNT(*) FROM resources WHERE active AND reservable) AS total_recursos,
       (SELECT COUNT(*) FROM resources WHERE active AND resource_type_code IN ('OPEN_WORKSPACE','CLOSED_OFFICE') AND public_qr_id IS NULL) AS oficinas_sin_qr,
       (SELECT COUNT(*) FROM resources WHERE active AND resource_type_code = 'MEETING_ROOM' AND public_qr_id IS NOT NULL) AS salas_con_qr,
       (SELECT COUNT(*) FROM checkins c JOIN resources r ON r.id = c.resource_id WHERE r.resource_type_code = 'MEETING_ROOM') AS checkins_en_salas;