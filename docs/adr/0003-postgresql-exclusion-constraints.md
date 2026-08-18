# ADR-0003: PostgreSQL Exclusion Constraints for Booking Conflicts

## Status
Accepted

## Context
The core business rule: **No two reservations can overlap for the same resource** (and same user).
This must be enforced under high concurrency (multiple users booking simultaneously).
Application-level locking is insufficient (distributed race conditions).
We need declarative, database-enforced prevention of double-booking.

## Decision
Use **PostgreSQL Exclusion Constraints** with **GIST index** and **tsrange** types.

### Implementation

```sql
-- Resource-level exclusion (no overlapping reservations for same resource)
ALTER TABLE reservations ADD CONSTRAINT ex_no_resource_overlap
EXCLUDE USING gist (
    resource_id WITH =,
    tsrange(reservation_date + start_time, reservation_date + end_time, '[)') WITH &&
)
WHERE (status IN ('CONFIRMED', 'CHECKED_IN'));

-- User-level exclusion (no overlapping reservations for same user)
ALTER TABLE reservations ADD CONSTRAINT ex_no_user_overlap
EXCLUDE USING gist (
    user_id WITH =,
    tsrange(reservation_date + start_time, reservation_date + end_time, '[)') WITH &&
)
WHERE (status IN ('CONFIRMED', 'CHECKED_IN'));
```

### Required Extensions
```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;  -- GIST support for = && operators
```

### How It Works
- `tsrange(start, end, '[)')` creates a half-open time range (inclusive start, exclusive end)
- `WITH &&` specifies the **overlaps** operator for ranges
- `WHERE` clause makes it a **partial index** - only active reservations constrained
- GIST index efficiently handles range overlap detection

## Consequences

### Positive
- **Race-Condition Proof**: Database engine enforces atomically, no application locks needed
- **Declarative**: Single DDL statement, no procedural code to maintain
- **Performance**: GIST index optimized for range queries; partial index reduces size
- **Correctness**: Impossible to violate via any path (API, direct SQL, migration scripts)
- **Simplicity**: Removes complex application-level concurrency logic

### Negative
- **PostgreSQL Specific**: Not portable to SQL Server/MySQL without rewrite
- **Error Handling**: Constraint violations return generic `PostgresException` (SQLSTATE 23P01)
  - Must map to user-friendly "Resource not available" / "You have a conflict"
- **Debugging**: Harder to inspect than application logic

### Neutral
- Requires `btree_gist` extension (standard in PostgreSQL 16)
- Application still validates early for UX (avoids round-trip on obvious conflicts)

## Alternatives Considered

1. **Application-Level Locking (Redis Distributed Lock)**
   - Rejected: Adds infrastructure complexity, lock expiration edge cases, single point of failure

2. **Serializable Transaction Isolation**
   - Rejected: High contention, serialization failures, performance impact

3. **Advisory Locks (pg_advisory_xact_lock)**
   - Rejected: Manual lock management, easy to forget, same race condition risks

4. **Trigger-Based Validation**
   - Rejected: Row-level triggers fire per-row, slower, harder to maintain, same race conditions

## References
- [PostgreSQL Exclusion Constraints](https://www.postgresql.org/docs/current/ddl-constraints.html#DDL-CONSTRAINTS-EXCLUSION)
- [btree_gist Extension](https://www.postgresql.org/docs/current/btree-gist.html)
- [Range Types](https://www.postgresql.org/docs/current/rangetypes.html)
- [Preventing Double-Booking with Exclusion Constraints](https://wiki.postgresql.org/wiki/What_is_new_in_PostgreSQL_9.2#Exclusion_Constraints)