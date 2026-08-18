# ADR-0005: Hybrid RBAC + ABAC Authorization Model

## Status
Accepted

## Context
Authorization requirements:
- **Administrative Roles**: GLOBAL_ADMIN, ROOM_ADMIN, SUPPORT, USER (hierarchical)
- **Business Profiles**: COLLABORATOR, ASSOCIATE, LEADER, DIRECTOR, PARTNER (functional)
- **Resource-Type Permissions**: Different profiles can book different resource types
- **Exceptions**: Time-bound overrides (e.g., ROOM_ADMIN unlimited meeting rooms only)
- **Minimum Privilege**: SUPPORT cannot manage roles or create exceptions

Pure RBAC or pure ABAC insufficient for this matrix.

## Decision
Implement **Hybrid Authorization**: RBAC for administrative boundaries + ABAC (Attribute-Based) for resource access policies.

### Model

```
┌─────────────────────────────────────────────────────────────┐
│                    AUTHORIZATION DECISION                    │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
    ┌─────────────────────┐         ┌─────────────────────┐
    │     RBAC Layer      │         │     ABAC Layer      │
    │ (Application Roles) │         │ (Business Profiles) │
    └─────────────────────┘         └─────────────────────┘
              │                               │
              ▼                               ▼
    ┌─────────────────────┐         ┌─────────────────────┐
    │ GLOBAL_ADMIN: All   │         │ Profile × Resource  │
    │ ROOM_ADMIN: Rooms   │         │ Type → Permissions  │
    │ SUPPORT: Modify     │         │ (can_view,          │
    │ USER: Standard      │         │  can_reserve,       │
    │                     │         │  can_modify_own)    │
    └─────────────────────┘         └─────────────────────┘
              │                               │
              └───────────────┬───────────────┘
                              ▼
                    ┌─────────────────────┐
                    │   EXCEPTION Layer   │
                    │ (Time-bound overrides)    │
                    └─────────────────────┘
```

### Database Schema

```sql
-- Administrative Roles (RBAC)
application_roles (code, name, description)
user_application_roles (user_id, role_code, valid_from, expires_at)

-- Business Profiles (ABAC Attributes)
business_profiles (code, name)
user_business_profiles (user_id, profile_code, valid_from, expires_at)

-- Policy Matrix (ABAC Rules)
resource_access_policies (
    resource_type_code,      -- OPEN_WORKSPACE, CLOSED_OFFICE, MEETING_ROOM
    business_profile_code,   -- COLLABORATOR...PARTNER
    can_view, can_reserve, can_modify_own
)

-- Exceptions (Overrides)
reservation_exceptions (
    user_id,
    maximum_future_active_reservations,
    applies_to_resource_type_code,  -- NULL = all types
    valid_from, expires_at, reason
)
```

### Authorization Flow

```csharp
// 1. Check Administrative Role (RBAC)
if (User.HasRole("GLOBAL_ADMIN")) return Allow;

// 2. Check Exception (Override)
var exception = await GetActiveException(userId, resourceType);
if (exception != null) return ApplyException(exception);

// 3. Check Business Profile Policy (ABAC)
var canReserve = await _policyService.CanReserve(userId, resourceTypeCode);
return canReserve ? Allow : Deny;
```

### Permission Matrix (from FRD)

| Profile | OPEN_WORKSPACE | CLOSED_OFFICE | MEETING_ROOM |
|---------|----------------|---------------|--------------|
| COLLABORATOR | ✓ Reserve | ✗ Reserve | ✓ Reserve |
| ASSOCIATE | ✓ Reserve | ✗ Reserve | ✓ Reserve |
| LEADER | ✓ Reserve | ✓ Reserve | ✓ Reserve |
| DIRECTOR | ✓ Reserve | ✓ Reserve | ✓ Reserve |
| PARTNER | ✓ Reserve | ✓ Reserve | ✓ Reserve |

### Exception Rules
- **ROOM_ADMIN**: Unlimited future reservations **only** for `MEETING_ROOM`
- **GLOBAL_ADMIN**: No limits, all resource types
- **Custom Exceptions**: Admin-defined, time-bound, per-user, per-resource-type

## Consequences

### Positive
- **Separation of Concerns**: Admin roles ≠ Business permissions
- **Flexibility**: Policy matrix editable without code changes
- **Auditability**: Clear why access granted (role vs profile vs exception)
- **Minimum Privilege**: SUPPORT can modify reservations but not manage roles
- **Scalability**: New resource types or profiles added via data, not code

### Negative
- **Complexity**: Three-layer evaluation (Role → Exception → Policy)
- **Performance**: Multiple DB lookups (cached in practice)
- **Testing**: More combinations to verify

### Neutral
- Requires caching strategy for policies/exceptions
- Admin UI needed for policy matrix management

## Alternatives Considered

1. **Pure RBAC (Roles Only)**
   - Rejected: Would need roles like `LEADER_OPEN_WORKSPACE`, `LEADER_CLOSED_OFFICE` - combinatorial explosion

2. **Pure ABAC (Attributes Only)**
   - Rejected: Administrative boundaries (GLOBAL_ADMIN vs SUPPORT) don't map cleanly to attributes

3. **Policy-Based Authorization (ASP.NET Core Policies Only)**
   - Rejected: Policies are code-defined; business matrix needs to be data-driven

## References
- [NIST RBAC Model](https://csrc.nist.gov/projects/role-based-access-control)
- [ABAC by NIST](https://csrc.nist.gov/projects/attribute-based-access-control)
- [ASP.NET Core Authorization Policies](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)