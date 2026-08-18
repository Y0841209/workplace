# Workplace Booking Platform - Domain Review

## Executive Summary

This document provides a comprehensive architectural review of the Workplace Booking Platform domain model. The system is designed for a legal firm to manage workspace reservations (open workspaces, closed offices, and meeting rooms) with QR-based check-in, role-based access control, and audit trail capabilities.

---

## 1. All Entities

### Core Domain Entities (18 Total)

| Entity | Type | Description |
|--------|------|-------------|
| **AppUser** | AggregateRoot | Users authenticated via Microsoft Entra ID |
| **AppSettings** | Entity | Global configuration singleton |
| **Location** | Entity | Physical office locations (multi-tenant ready) |
| **Floor** | Entity | Building floors within locations |
| **Zone** | Entity | Logical groupings within floors |
| **ResourceType** | Entity | Catalog: OPEN_WORKSPACE, CLOSED_OFFICE, MEETING_ROOM |
| **Resource** | Entity | Bookable spaces (offices, meeting rooms) |
| **BusinessProfile** | Entity | Functional roles: COLLABORATOR, ASSOCIATE, LEADER, DIRECTOR, PARTNER |
| **ApplicationRole** | Entity | Admin roles: USER, ROOM_ADMIN, SUPPORT, GLOBAL_ADMIN |
| **UserBusinessProfile** | Entity | User ↔ BusinessProfile assignment with validity period |
| **UserApplicationRole** | Entity | User ↔ ApplicationRole assignment with validity period |
| **ResourceAccessPolicy** | Entity | Profile × ResourceType permission matrix |
| **ReservationException** | Entity | Temporary limit overrides per user/resource type |
| **Reservation** | AggregateRoot | Core booking entity with full lifecycle |
| **CheckIn** | Entity | QR-based check-in records |
| **NotificationOutbox** | Entity | Transactional outbox for email notifications |
| **AuditLog** | Entity | Immutable audit trail for sensitive operations |
| **CheckIn** | Entity | QR-based check-in confirmation |

---

## 2. Aggregate Roots

| Aggregate Root | Entity | Consistency Boundary | Domain Events Raised |
|----------------|--------|---------------------|---------------------|
| **AppUser** | AppUser | User identity & Entra ID sync | - |
| **Reservation** | Reservation | Full reservation lifecycle | ReservationCreatedEvent, ReservationModifiedEvent, ReservationCancelledEvent, CheckInCompletedEvent, CheckOutCompletedEvent |
| **Resource** | Resource | Resource configuration & QR policy | ResourceCreatedEvent, ResourceModifiedEvent, ResourceDeletedEvent |

### Aggregate Root Characteristics

| Aggregate | ID Type | Invariants Enforced | Domain Events |
|-----------|---------|---------------------|---------------|
| **AppUser** | Guid | EntraObjectId unique, Email unique | - |
| **Resource** | Guid | QR policy per type, capacity > 0, code unique | ResourceCreated/Modified/DeletedEvent |
| **Reservation** | Guid | Time ordering, duration ≥ 1hr, end ≤ 23:59, no overlaps, same-day | ReservationCreated/Modified/Cancelled/CheckIn/CheckOutEvent |

---

## 3. Value Objects

| Value Object | Location | Description |
|--------------|----------|-------------|
| **DateOnly** | System | Reservation date (no time component) |
| **TimeOnly** | System | Start/end times (no date component) |
| **DateTimeOffset** | System | Timestamp with timezone (UTC stored) |
| **Guid** | System | All entity identifiers |
| **Email** | Citext (DB) | Case-insensitive email (AppUser) |
| **TimeOnly** | System | Reservation start/end times |
| **DateOnly** | System | Reservation date |

> **Note**: No explicit custom Value Object classes exist. Primitive types with semantic meaning are used directly with validation in entity factories.

---

## 4. Enumerations

### Domain Enums

| Enum | Values | Usage |
|------|--------|-------|
| **ResourceTypeCode** | `OPEN_WORKSPACE`, `CLOSED_OFFICE`, `MEETING_ROOM` | Resource classification |
| **ReservationStatus** | `CONFIRMED`, `CHECKED_IN`, `CHECKED_OUT`, `CANCELLED`, `COMPLETED`, `NOT_CHECKED_IN`, `REJECTED` | Reservation lifecycle |
| **CheckInMethod** | `QR` | Check-in method (extensible) |
| **NotificationType** | `RESERVATION_CREATED`, `RESERVATION_MODIFIED`, `RESERVATION_CANCELLED`, `RESERVATION_REMINDER` | Notification categorization |
| **NotificationStatus** | `PENDING`, `SENT`, `FAILED`, `CANCELLED` | Outbox processing state |
| **BusinessProfileCode** | `COLLABORATOR`, `ASSOCIATE`, `LEADER`, `DIRECTOR`, `PARTNER` | Functional hierarchy |
| **ApplicationRoleCode** | `USER`, `ROOM_ADMIN`, `SUPPORT`, `GLOBAL_ADMIN` | Administrative hierarchy |
| **NotificationStatus** | `PENDING`, `SENT`, `FAILED`, `CANCELLED` | Outbox processing |
| **CheckInMethod** | `QR` | Check-in mechanism |
| **ReservationStatus** | 7 values | Reservation lifecycle |

### Enum Characteristics

| Enum | Storage | Extensibility |
|------|---------|---------------|
| ResourceTypeCode | TEXT (PK) | Fixed - DB enum |
| ReservationStatus | TEXT | Fixed - DB enum |
| BusinessProfileCode | TEXT (PK) | Configurable |
| ApplicationRoleCode | TEXT (PK) | Configurable |

---

## 5. Relationships

### Entity Relationship Diagram (Textual)

```
Location (1) ─────< (N) Floor
Floor (1) ─────< (N) Zone
Floor (1) ─────< (N) Resource
Zone (1) ─────< (N) Resource
ResourceType (1) ─────< (N) Resource
ResourceType (1) ─────< (N) ResourceAccessPolicy
Location (1) ─────< (N) Resource

AppUser (1) ─────< (N) UserBusinessProfile >──── (N) BusinessProfile
AppUser (1) ─────< (N) UserApplicationRole >──── (N) ApplicationRole
AppUser (1) ─────< (N) Reservation (as User)
AppUser (1) ─────< (N) Reservation (as CreatedByUser)
AppUser (1) ─────< (N) Reservation (as CancelledByUser)
AppUser (1) ─────< (N) CheckIn
AppUser (1) ─────< (N) NotificationOutbox
AppUser (1) ─────< (N) AuditLog (actor)
AppUser (1) ─────< (N) UserBusinessProfile (assigned by)
AppUser (1) ─────< (N) UserApplicationRole (assigned by)
AppUser (1) ─────< (N) ReservationException (created by)

ResourceType (1) ─────< (N) ResourceAccessPolicy >──── (N) BusinessProfile
ResourceType (1) ─────< (N) ReservationException (applies to)

Resource (1) ─────< (N) Reservation
Resource (1) ─────< (N) CheckIn
Reservation (1) ──── (1) CheckIn
Resource (1) ─────< (N) ReservationException (applies to)

Reservation (1) ──── (1) CheckIn
Reservation (1) ─────< (N) NotificationOutbox
Reservation (1) ─────< (N) AuditLog (entity)

AuditLog (N) ── Actor (AppUser)
NotificationOutbox (N) ── Recipient (AppUser)
```

### Relationship Cardinalities

| Parent | Child | Cardinality | Cascade Delete |
|--------|-------|-------------|----------------|
| Location | Floor | 1:N | RESTRICT |
| Floor | Zone | 1:N | RESTRICT |
| Floor | Resource | 1:N | RESTRICT |
| Zone | Resource | 1:N | SET NULL |
| ResourceType | Resource | 1:N | RESTRICT |
| ResourceType | ResourceAccessPolicy | 1:N | RESTRICT |
| BusinessProfile | ResourceAccessPolicy | 1:N | RESTRICT |
| BusinessProfile | UserBusinessProfile | 1:N | RESTRICT |
| ApplicationRole | UserApplicationRole | 1:N | RESTRICT |
| AppUser | UserBusinessProfile | 1:N | CASCADE |
| AppUser | UserApplicationRole | 1:N | CASCADE |
| AppUser | Reservation | 1:N | RESTRICT |
| AppUser | CheckIn | 1:N | RESTRICT |
| Resource | Reservation | 1:N | RESTRICT |
| Reservation | CheckIn | 1:1 | CASCADE |
| Reservation | NotificationOutbox | 1:N | SET NULL |

---

## 6. Business Invariants

### Reservation Invariants (Enforced in `Reservation.Create` & `Modify`)

| Invariant | Description | Enforcement |
|-----------|-------------|-------------|
| **Time Ordering** | EndTime > StartTime | Factory validation + CHECK constraint |
| **Minimum Duration** | Duration ≥ 1 hour | Factory + CHECK constraint |
| **Maximum End Time** | EndTime ≤ 23:59 | Factory + CHECK constraint |
| **Same Day** | Start and End on same calendar day | Factory validation |
| **Past Date Prevention** | ReservationDate ≥ Today | Factory validation |
| **Attendee Count** | > 0 when specified | Factory + CHECK constraint |
| **Meeting Room Capacity** | AttendeeCount ≤ Resource.Capacity | Factory validation |
| **No Double Booking (Resource)** | No overlapping reservations for same resource | EXCLUSION CONSTRAINT (GIST) |
| **No Double Booking (User)** | No overlapping reservations for same user | EXCLUSION CONSTRAINT (GIST) |

### Resource Invariants

| Invariant | Description | Enforcement |
|-----------|-------------|-------------|
| **QR Policy** | OPEN/CLOSED require QR; MEETING_ROOM forbids QR | Factory + CHECK constraint |
| **Capacity** | Must be > 0 | Factory + CHECK constraint |
| **Code Uniqueness** | Unique across all resources | UNIQUE INDEX |
| **QR Uniqueness** | PublicQrId unique when present | UNIQUE INDEX |

### User & Authorization Invariants

| Invariant | Description | Enforcement |
|-----------|-------------|-------------|
| **Entra ID Uniqueness** | One AppUser per Entra ObjectId | UNIQUE INDEX |
| **Email Uniqueness** | Case-insensitive email unique | UNIQUE INDEX (citext) |
| **Active Profile Uniqueness** | One active profile per user per type | Partial UNIQUE INDEX |
| **Active Role Uniqueness** | One active role per user per type | Partial UNIQUE INDEX |
| **Reservation Ownership** | Only owner/SUPPORT can modify/cancel | Domain logic in methods |
| **Support Reason Required** | SUPPORT must provide reason | Domain logic |
| **ROOM_ADMIN Exception** | Unlimited reservations only for MEETING_ROOM | Policy service |

### State Machine: ReservationStatus

```
CONFIRMED ──CheckIn──► CHECKED_IN ──CheckOut──► CHECKED_OUT ──AutoComplete──► COMPLETED
     │                     │                          │
     └──Cancel────────────► CANCELLED                  │
                           │                          │
                           └──AutoComplete (no check-in)─► NOT_CHECKED_IN
```

### Valid Transitions

| From | To | Trigger |
|------|-----|---------|
| CONFIRMED | CHECKED_IN | User CheckIn (within ±15min window) |
| CONFIRMED | CANCELLED | User/SUPPORT Cancel |
| CONFIRMED | NOT_CHECKED_IN | AutoComplete (no check-in) |
| CHECKED_IN | CHECKED_OUT | User CheckOut / AutoComplete |
| CHECKED_IN | CANCELLED | SUPPORT Cancel |
| CHECKED_OUT | COMPLETED | AutoComplete |
| NOT_CHECKED_IN | COMPLETED | AutoComplete |
| CANCELLED | - | Terminal |

### Check-In Invariants

| Invariant | Description | Enforcement |
|-----------|-------------|-------------|
| **Resource Type** | Only OPEN_WORKSPACE / CLOSED_OFFICE | Handler + Trigger |
| **QR Match** | Scanned QR = Resource.PublicQrId | Handler |
| **Ownership** | Reservation.UserId = CurrentUser | Handler |
| **Status** | Must be CONFIRMED | Handler |
| **Date** | ReservationDate = Today | Handler |
| **Time Window** | Now ∈ [Start-15min, End+15min] | Handler |

### Audit Invariants

| Invariant | Description |
|-----------|-------------|
| **Immutability** | AuditLog never updated/deleted |
| **Correlation** | CorrelationId links related operations |
| **Before/After** | JSON snapshots for mutations |
| **Actor Tracking** | ActorUserId nullable (system actions) |

---

## Architectural Observations

### Strengths

1. **Rich Domain Model** - Entities encapsulate behavior, not just data
2. **Database-Enforced Invariants** - Exclusion constraints prevent race conditions
3. **Explicit State Machine** - ReservationStatus transitions are explicit
4. **Audit Trail** - Comprehensive immutable audit log
5. **Outbox Pattern** - Reliable notification delivery
5. **QR Security** - PublicQrId rotation, time-window validation
6. **Hierarchical Authorization** - Profile × ResourceType policy matrix

### Areas for Consideration

| Area | Observation | Recommendation |
|--------|-------------|----------------|
| **Value Objects** | No explicit VO classes | Consider DateRange, Capacity, QRCode VOs |
| **Time Zone** | All times stored as TimeOnly (UTC assumed) | Document timezone strategy |
| **Cross-Day** | Not supported (AppSettings.AllowCrossDayBooking=false) | Document limitation |
| **Recurring** | Not supported | Future enhancement |
| **Waitlist** | Not implemented | Future enhancement |

---

## Summary Statistics

| Metric | Count |
|--------|-------|
| Entities | 18 |
| Aggregate Roots | 3 |
| Domain Events | 11 |
| Enumerations | 9 |
| Relationships | 22 |
| Domain Services | 4 |
| Specifications | 12 |

---

*Document generated from domain model analysis. Last updated: 2026-08-15*