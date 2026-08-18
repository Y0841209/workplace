# ADR-0007: QR Check-in with Public UUID per Resource

## Status
Accepted

## Context
QR Check-in requirements:
- Only for OPEN_WORKSPACE and CLOSED_OFFICE (not MEETING_ROOM)
- QR code on office door/scanner
- User scans with phone → opens web URL → authenticates → confirms check-in
- Validate: user has active reservation for that resource, within time window
- No credentials or PII in QR code
- QR rotation capability (reprint codes)

## Decision
Each office resource gets a **`public_qr_id` (UUID v4)** stored in `resources` table. Meeting rooms have `NULL`.

### QR Code Content
```
https://booking.company.com/check-in/{public_qr_id}
```
- No tokens, no user info, no resource IDs
- Just a random UUID v4 (122 bits entropy)

### Database Constraint
```sql
CONSTRAINT ck_resource_qr_policy CHECK (
    (resource_type_code IN ('OPEN_WORKSPACE', 'CLOSED_OFFICE') AND public_qr_id IS NOT NULL)
    OR (resource_type_code = 'MEETING_ROOM' AND public_qr_id IS NULL)
)
```

### Rotation Support
```sql
qr_version integer NOT NULL DEFAULT 1,
```
- Increment `qr_version`, generate new `public_qr_id` → old codes invalid
- Admin UI: "Regenerate QR" button per resource

### Check-in Flow

```
1. User scans QR → Frontend: GET /check-in/{publicQrId}
2. Backend: Resolve resource by public_qr_id
3. Backend: Find active reservation for THIS user, THIS resource, NOW ±15min
4. Frontend: Show "Check-in" button (if valid reservation found)
5. User taps → POST /reservations/{id}/check-in
6. Backend: Validate ownership, time window, resource type requires check-in
7. Backend: INSERT checkins (unique reservation_id), UPDATE reservation status
8. Return success
```

### Security Validations
- **Ownership**: `reservation.user_id == current_user.id`
- **Resource Match**: `reservation.resource_id == resolved_resource.id`
- **Time Window**: `NOW() BETWEEN start_time - 15min AND end_time`
- **Resource Type**: Only `OPEN_WORKSPACE` or `CLOSED_OFFICE`
- **Status**: Reservation must be `CONFIRMED`

### Check-in Table
```sql
CREATE TABLE checkins (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reservation_id uuid NOT NULL UNIQUE REFERENCES reservations(id),
    resource_id uuid NOT NULL REFERENCES resources(id),
    user_id uuid NOT NULL REFERENCES app_users(id),
    method checkin_method NOT NULL DEFAULT 'QR',
    scanned_public_qr_id uuid NOT NULL,
    checked_in_at timestamptz NOT NULL DEFAULT now(),
    ip_address inet,
    user_agent text
);
```

## Consequences

### Positive
- **Security**: QR contains zero sensitive data (just random UUID)
- **Rotation**: `qr_version` enables invalidating old printed codes
- **Constraint**: DB enforces QR only for offices (not meeting rooms)
- **Audit**: Full check-in trail (user, resource, time, IP, UA)
- **Simplicity**: Single endpoint resolves QR → shows reservation

### Negative
- **Public UUID Guessable**: 122-bit entropy, but theoretically enumerable
  - Mitigation: Rate limiting on `/check-in/{id}` endpoint
- **No Offline Validation**: Requires network (by design - real-time validation)
- **Meeting Rooms Excluded**: By policy, not technical limitation

### Neutral
- QR codes printed physically (sticker on door) or digital (tablet)
- Frontend handles "no active reservation" UX gracefully

## Alternatives Considered

1. **Signed JWT in QR**
   - Rejected: Overkill, larger QR, key rotation complexity, no offline benefit

2. **Resource Code in QR (e.g., `P03-OA-001`)**
   - Rejected: Predictable, leaks inventory structure, no rotation

3. **One-Time QR per Reservation**
   - Rejected: Requires pre-generation, complex for walk-up use, printing logistics

4. **NFC Tags**
   - Rejected: Hardware cost, phone compatibility, same security model

## References
- [UUID v4 Entropy](https://datatracker.ietf.org/doc/html/rfc4122#section-4.4)
- [OWASP QR Code Security](https://owasp.org/www-project-mobile-top-10/)