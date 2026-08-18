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