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