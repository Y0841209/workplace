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