-- Database Extensions Initialization
-- Run as superuser during container initialization

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "btree_gist";
CREATE EXTENSION IF NOT EXISTS "citext";

-- Grant usage to application user
-- (Run after database and user are created)
-- GRANT USAGE ON SCHEMA public TO booking_user;
-- GRANT CREATE ON SCHEMA public TO booking_user;