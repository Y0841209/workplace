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
