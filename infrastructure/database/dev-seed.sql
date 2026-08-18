-- Development Seed Data
-- Only runs in development environment (docker-compose.override.yml)

-- Create a test user for development
INSERT INTO booking.app_users (
    id,
    entra_object_id,
    email,
    display_name,
    job_title,
    department,
    active
) VALUES (
    '11111111-1111-1111-1111-111111111111',
    '22222222-2222-2222-2222-222222222222',
    'dev.user@company.com',
    'Development User',
    'Software Engineer',
    'Engineering',
    true
) ON CONFLICT (entra_object_id) DO NOTHING;

-- Assign GLOBAL_ADMIN role to dev user
INSERT INTO booking.user_application_roles (
    id,
    user_id,
    role_code,
    valid_from,
    active,
    assigned_by_user_id,
    assignment_reason
) VALUES (
    gen_random_uuid(),
    '11111111-1111-1111-1111-111111111111',
    'GLOBAL_ADMIN',
    CURRENT_DATE,
    true,
    '11111111-1111-1111-1111-111111111111',
    'Development admin access'
) ON CONFLICT DO NOTHING;

-- Assign LEADER business profile
INSERT INTO booking.user_business_profiles (
    id,
    user_id,
    profile_code,
    valid_from,
    active,
    assigned_by_user_id,
    assignment_reason
) VALUES (
    gen_random_uuid(),
    '11111111-1111-1111-1111-111111111111',
    'LEADER',
    CURRENT_DATE,
    true,
    '11111111-1111-1111-1111-111111111111',
    'Development leader access'
) ON CONFLICT DO NOTHING;

-- Create some test reservations for today
INSERT INTO booking.reservations (
    id,
    resource_id,
    user_id,
    created_by_user_id,
    reservation_date,
    start_time,
    end_time,
    status,
    title,
    description,
    attendee_count
) 
SELECT 
    gen_random_uuid(),
    r.id,
    '11111111-1111-1111-1111-111111111111',
    '11111111-1111-1111-1111-111111111111',
    CURRENT_DATE,
    '09:00',
    '11:00',
    'CONFIRMED',
    'Development test reservation',
    'Test reservation for development',
    1
FROM booking.resources r
WHERE r.resource_type_code = 'OPEN_WORKSPACE'
LIMIT 3
ON CONFLICT DO NOTHING;

-- Create a meeting room reservation
INSERT INTO booking.reservations (
    id,
    resource_id,
    user_id,
    created_by_user_id,
    reservation_date,
    start_time,
    end_time,
    status,
    title,
    description,
    attendee_count
)
SELECT 
    gen_random_uuid(),
    r.id,
    '11111111-1111-1111-1111-111111111111',
    '11111111-1111-1111-1111-111111111111',
    CURRENT_DATE,
    '14:00',
    '15:00',
    'CONFIRMED',
    'Team meeting',
    'Development team sync',
    5
FROM booking.resources r
WHERE r.code = 'SJ-01'
LIMIT 1
ON CONFLICT DO NOTHING;