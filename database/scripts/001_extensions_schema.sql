-- 001_extensions_schema.sql
-- Extensiones PostgreSQL requeridas y creación del esquema booking
-- Requiere permisos de superusuario (usuario POSTGRES_USER del contenedor,
-- o en Render: los planes permiten pgcrypto/btree_gist/citext).

CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "btree_gist";
CREATE EXTENSION IF NOT EXISTS "citext";

-- Esquema usado por EF Core (HasDefaultSchema("booking")) y por los scripts 003-009
CREATE SCHEMA IF NOT EXISTS booking;
