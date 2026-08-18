# Workplace Booking Platform - Testing Strategy

## Overview

This document defines the comprehensive testing strategy for the Workplace Booking Platform, targeting **minimum 80% code coverage** with emphasis on critical business rules validation.

---

## Test Pyramid

```
        /\
       /  \     E2E / API Tests (10%)
      /----\    
     /      \   Integration Tests (20%)
    /--------\  
   /          \ Unit Tests (70%)
  /------------\
```

| Layer | Target % | Focus | Tools |
|-------|----------|-------|-------|
| **Unit Tests** | 70% | Domain logic, validators, services, handlers | xUnit, Moq, FluentAssertions |
| **Integration Tests** | 20% | Database, repositories, EF Core, specs | Testcontainers (PostgreSQL), WebApplicationFactory |
| **Architecture Tests** | 5% | Clean Architecture rules, dependencies | NetArchTest / ArchUnitNET |
| **API / E2E Tests** | 5% | Full HTTP flows, auth, contracts | Playwright, ASP.NET Core TestServer |

---

## 1. Unit Tests (70% Target)

### 1.1 Domain Entities Tests

| Entity | Test File | Key Scenarios |
|--------|-----------|---------------|
| **Reservation** | `ReservationTests.cs` | Create (valid/invalid), Modify, Cancel, CheckIn, CheckOut, AutoComplete, State transitions |
| **Resource** | `ResourceTests.cs` | Create (OA/OC/MeetingRoom), QR policy, Update, RegenerateQr, Capacity validation |
| **CheckIn** | `CheckInTests.cs` | Create valid/invalid, QR required, user/resource required |
| **ResourceType** | (new) | CRUD, QR/Checkin flags |
| **UserBusinessProfile** | (new) | Valid dates, IsActiveOn, expiration |
| **UserApplicationRole** | (new) | Valid dates, IsActiveOn, expiration |
| **ResourceAccessPolicy** | (new) | Permission matrix |
| **ReservationException** | (new) | Date validity, active on date |

#### Critical Business Rule Validations (Domain)

| Rule | Test Case | Expected |
|------|-----------|----------|
| **Min Duration 1hr** | `Create(9:00, 9:30)` | Failure `RESERVATION_MIN_DURATION` |
| **Max End 23:59** | `Create(22:00, 23:59:01)` | Failure `RESERVATION_MAX_END_TIME` |
| **Same Day** | `Create(9:00, 00:30 next day)` | Failure `RESERVATION_SAME_DAY` |
| **Time Order** | `Create(11:00, 10:00)` | Failure `TIME_ORDER_INVALID` |
| **Attendee ≤ Capacity** | MeetingRoom cap=5, attendees=6 | Failure `ATTENDEE_COUNT_EXCEEDS_CAPACITY` |

### 1.2 Application Layer Validators

| Validator | Test File | Scenarios |
|-----------|-----------|-----------|
| `CreateReservationValidator` | `CreateReservationValidatorTests.cs` | All sync rules + async (future limit, overlaps) |
| `UpdateReservationValidator` | (new) | Ownership, support reason, time rules |
| `CancelReservationValidator` | (new) | Ownership, support reason, valid status |
| `CreateResourceValidator` | `CreateResourceValidatorTests.cs` | Type enum, QR policy, capacity, FKs |
| `UpdateResourceValidator` | (new) | QR policy on type change |
| `CreateResourceDtoValidator` | (new) | Import batch validation |

### 1.3 Domain Services

| Service | Test File | Key Scenarios |
|---------|-----------|---------------|
| `ReservationPolicyService` | `ReservationPolicyServiceTests.cs` | Max reservations, GLOBAL_ADMIN bypass, ROOM_ADMIN exception (MEETING_ROOM only), exceptions |
| `AvailabilityService` | `AvailabilityServiceTests.cs` | Active/reservable check, overlap detection, exclude self |
| `ReservationPolicyService` | (extend) | `CanReserveAsync` profile×resource matrix |

### 1.4 Application Handlers (CQRS)

| Handler | Test File | Key Scenarios |
|---------|-----------|---------------|
| `CreateReservationHandler` | `CreateReservationHandlerTests.cs` | Valid, conflict, limits, attendee>capacity, auth |
| `UpdateReservationHandler` | (new) | Owner/support, time changes, attendee changes |
| `CancelReservationHandler` | (new) | Owner/support, reason, status checks |
| `CheckInReservationHandler` | `CheckInReservationHandlerTests.cs` | Valid, not owner, wrong QR, meeting room, not today, time window |
| `CheckOutReservationHandler` | (new) | Owner, status CHECKED_IN only |
| `GetMyReservationsHandler` | (new) | Pagination, filters, ownership |
| `GetAvailabilityHandler` | (new) | Filters, real-time availability |

### 1.5 Domain Services

| Service | Test File | Scenarios |
|---------|-----------|-----------|
| `ReservationPolicyService` | `ReservationPolicyServiceTests.cs` | Max future (5), GLOBAL_ADMIN bypass, ROOM_ADMIN only MEETING_ROOM, exceptions |
| `AvailabilityService` | `AvailabilityServiceTests.cs` | Resource active, overlaps, exclude self |

---

## 2. Integration Tests (20% Target)

### 2.1 Repository & Persistence

| Test | Scenario |
|------|----------|
| `ReservationRepositoryTests` | CRUD, CountAsync, ListAsync with specs, overlapping detection |
| `ResourceRepositoryTests` | CRUD, GetByPublicQrId, availability queries |
| `UserRepositoryTests` | GetByEntraId, GetByEmail, profiles/roles loading |
| `SpecificationEvaluatorTests` | Complex specs translation to SQL |

### 2.2 Database Constraints & Triggers

| Test | Scenario | Expected |
|------|----------|----------|
| `ExclusionConstraintTests` | Double booking same resource | PG Exception 23P01 |
| `ExclusionConstraintTests` | Double booking same user | PG Exception 23P01 |
| `CheckConstraintsTests` | Duration < 1hr | CHECK violation |
| `CheckConstraintsTests` | End > 23:59 | CHECK violation |
| `QRPolicyTriggerTests` | OA/OC without QR | CHECK violation |
| `QRPolicyTriggerTests` | MeetingRoom with QR | CHECK violation |
| `CheckInTriggerTests` | Check-in on meeting room | Trigger error |
| `CheckInTriggerTests` | Check-in wrong user | Trigger error |
| `CheckInTriggerTests` | Check-in wrong date | Trigger error |

### 2.3 Domain Event Dispatch

| Test | Scenario |
|------|----------|
| `DomainEventDispatcherTests` | Events captured in SaveChanges, dispatched after commit |
| `AuditLoggingTests` | AuditLog created for all mutations |

### 2.4 Background Jobs (Hangfire)

| Job | Test |
|-----|------|
| `NotificationProcessor` | Processes pending, retries, dead-letter |
| `ReminderProcessor` | Finds reservations 15min ahead, enqueues reminders |

---

## 3. Architecture Tests (5% Target)

### 3.1 Clean Architecture Rules (NetArchTest)

| Rule | Assembly | Expected |
|------|----------|----------|
| Domain has no external deps | `WorkplaceBooking.Domain` | Only `System.*`, `WorkplaceBooking.SharedKernel` |
| Application references only Domain/SharedKernel | `WorkplaceBooking.Application` | No `Infrastructure`, `Api` |
| Infrastructure implements Application interfaces | `WorkplaceBooking.Infrastructure` | Implements `IRepository`, `IEmailService`, etc. |
| API references only Application/Infrastructure | `WorkplaceBooking.Api` | No direct Domain usage in controllers |
| No circular dependencies | All | Acyclic |

### 3.2 Layer Isolation

| Layer | Allowed References | Forbidden |
|-------|-------------------|-----------|
| Domain | SharedKernel, BCL | Application, Infrastructure, Api |
| Application | Domain, SharedKernel | Infrastructure, Api |
| Infrastructure | Domain, Application, SharedKernel | Api |
| Api | Application, Infrastructure | Domain (direct) |

---

## 4. API Tests (5% Target)

### 4.1 Controller Tests (Integration with TestServer)

| Controller | Test File | Endpoints |
|------------|-----------|-----------|
| `ResourcesController` | `ResourcesControllerTests.cs` | CRUD, availability, by-floor, meeting-rooms, import |
| `ReservationsController` | `ReservationsControllerTests.cs` | CRUD, check-in/out, availability, check-in QR |
| `CheckInsController` | `CheckInControllerTests.cs` | History, resource check-ins, today |
| `UsersController` | `UsersControllerTests.cs` | Profile, roles, profiles, exceptions |

### 4.2 Critical API Scenarios (Playwright/E2E)

| Flow | Steps | Validation |
|------|-------|------------|
| **Full Reservation** | Login → Search availability → Create → Check-in → Check-out | 201, 200, 204 |
| **Conflict Detection** | Create overlapping → 409 | Conflict response |
| **QR Check-in** | Scan QR → Confirm → 200 | CheckInDto returned |
| **ROOM_ADMIN** | Create meeting room >5 | 201 (bypass limit) |
| **RL Exceeded** | Create 6th reservation | 403 Forbidden |
| **Check-in Window** | Before 15min / After 15min | 400 BadRequest |

---

## Critical Business Rules - Test Matrix

| Rule ID | Description | Unit | Integration | API | Priority |
|---------|-------------|------|-------------|-----|----------|
| **BR-001** | Min 1 hour duration | ✅ | ✅ | ✅ | P0 |
| **BR-002** | Max end 23:59 | ✅ | ✅ | ✅ | P0 |
| **BR-003** | Max 5 future active | ✅ | ✅ | ✅ | P0 |
| **BR-004** | ROOM_ADMIN exception only MEETING_ROOM | ✅ | ✅ | ✅ | P0 |
| **BR-005** | QR only OA/OC | ✅ | ✅ | ✅ | P0 |
| **BR-006** | MEETING_ROOM no check-in | ✅ | ✅ | ✅ | P0 |
| **BR-005** | No overlapping (resource) | ✅ | ✅ | ✅ | P0 |
| **BR-006** | No overlapping (user) | ✅ | ✅ | ✅ | P0 |
| **BR-007** | Same day only | ✅ | ✅ | ✅ | P0 |
| **BR-008** | Attendee ≤ capacity (rooms) | ✅ | | ✅ | P1 |
| **BR-009** | ROOM_ADMIN exception only MEETING_ROOM | ✅ | ✅ | | P0 |
| **BR-010** | QR rotation on regenerate | | ✅ | | P1 |
| **BR-011** | Check-in window ±15min | ✅ | ✅ | ✅ | P0 |
| **BR-012** | Same-day only | ✅ | | | P0 |
| **BR-011** | Check-in ownership | ✅ | ✅ | | P0 |

---

## Test Infrastructure

### 4.1 Test Project Structure

```
tests/
├── WorkplaceBooking.UnitTests/           # Domain + Application unit tests
│   ├── Domain/
│   ├── Application/
│   │   ├── Validators/
│   │   ├── Handlers/
│   │   └── Services/
│   └── Common/
├── WorkplaceBooking.IntegrationTests/    # DB + Repositories + Handlers
│   ├── Repositories/
│   ├── Handlers/
│   ├── Database/
│   └── Fixtures/
├── WorkplaceBooking.ArchitectureTests/   # NetArchTest rules
└── WorkplaceBooking.Api.Tests/           # Controller + E2E
    ├── Controllers/
    ├── E2E/
    └── Contracts/
```

### 4.2 Shared Test Fixtures

| Fixture | Purpose |
|---------|---------|
| `PostgreSqlFixture` | Testcontainers PostgreSQL 16 |
| `AppDbContextFixture` | Seeded AppDbContext per test |
| `CurrentUserFixture` | Mock ICurrentUserService |
| `MediatorFixture` | Configured IMediator with pipeline |
| `AutoMapperFixture` | Configured mapper profiles |

### 4.3 Test Data Builders

```csharp
// Fluent builders for test data
var resource = ResourceBuilder.Create()
    .AsOpenWorkspace()
    .OnFloor(3)
    .WithCapacity(1)
    .Build();

var reservation = ReservationBuilder.Create()
    .ForResource(resource)
    .ForUser(user)
    .OnDate(DateOnly.FromDateTime(DateTime.Today.AddDays(1)))
    .From(9).To(11)
    .Build();
```

---

## CI/CD Pipeline Integration

### Pipeline Stages

```yaml
stages:
  - lint-and-format
  - unit-tests          # ~2 min, 70% coverage gate
  - integration-tests   # ~5 min, Testcontainers
  - architecture-tests  # ~30 sec
  - api-tests           # ~2 min
  - coverage-report     # Merge + threshold 80%
  - security-scan       # SAST/SCA
  - deploy-staging
```

### Coverage Gates

```yaml
# coverlet + reportgenerator
coverage:
  minimum: 80%
  branches: 70%
  lines: 80%
  functions: 80%
```

---

## Special Focus: Critical Rules Validation

### BR-001: Minimum 1 Hour Duration

```csharp
[Theory]
[InlineData(9, 0, 9, 30)]   // 30 min - FAIL
[InlineData(9, 0, 10, 0)]   // 1 hour - PASS
[InlineData(9, 0, 10, 30)]  // 1.5 hr - PASS
public void CreateReservation_ValidatesMinDuration(int sh, int sm, int eh, int em)
```

### BR-002: Max End 23:59

```csharp
[Theory]
[InlineData(22, 0, 23, 59)]  // PASS
[InlineData(22, 0, 23, 59, 1)] // FAIL - 1 second over
```

### BR-003: Max 5 Future Active

```csharp
[Fact]
public async Task Create_6thReservation_Fails() {
    // Create 5 confirmed future reservations
    // 6th should fail with "Maximum 5 future active reservations exceeded"
    // GLOBAL_ADMIN should bypass
    // ROOM_ADMIN bypass only for MEETING_ROOM
}
```

### BR-004: ROOM_ADMIN Exception Only MEETING_ROOM

```csharp
[Theory]
[InlineData("MEETING_ROOM", true)]   // ROOM_ADMIN can exceed
[InlineData("OPEN_WORKSPACE", false)] // ROOM_ADMIN cannot exceed
[InlineData("CLOSED_OFFICE", false)]  // ROOM_ADMIN cannot exceed
```

### BR-005: QR Only for OA/OC

```csharp
[Theory]
[InlineData("OPEN_WORKSPACE", true, true)]   // QR required
[InlineData("CLOSED_OFFICE", true, true)]    // QR required
[InlineData("MEETING_ROOM", false, false)]   // QR forbidden
```

### BR-006: MEETING_ROOM No Check-in

```csharp
[Fact]
public async Task CheckIn_MeetingRoom_Fails() {
    // Create MEETING_ROOM reservation
    // Attempt check-in → should fail with "Check-in only allowed for offices"
}
```

### BR-007: No Overlapping Reservations

```csharp
[Fact]
public async Task Create_OverlappingResource_Fails() {
    // Create reservation 9-11
    // Create overlapping 10-12 → Conflict (exclusion constraint)
}
```

### BR-008: Attendee Count ≤ Capacity

```csharp
[Theory]
[InlineData(10, 5, false)]   // 10 attendees, cap 5 → FAIL
[InlineData(5, 10, true)]    // 5 attendees, cap 10 → PASS
```

---

## Test Execution Commands

```bash
# Unit tests only (fast)
dotnet test --filter "FullyQualifiedName~UnitTests" --collect:"XPlat Code Coverage"

# Integration tests (requires Docker)
dotnet test --filter "FullyQualifiedName~IntegrationTests"

# Architecture tests
dotnet test --filter "FullyQualifiedName~ArchitectureTests"

# API tests
dotnet test --filter "FullyQualifiedName~Api.Tests"

# All with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Coverage report
reportgenerator -reports:./TestResults/**/coverage.cobertura.xml \
  -targetdir:./coverage-report -reporttypes:Html
```

---

## Quality Gates (PR Pipeline)

| Gate | Threshold | Tool |
|------|-----------|------|
| Unit Test Coverage | ≥ 80% | coverlet |
| Integration Coverage | ≥ 60% | coverlet |
| Branch Coverage | ≥ 70% | coverlet |
| Architecture Violations | 0 | NetArchTest |
| Critical Rule Tests | 100% pass | xUnit |
| Security Scan | 0 High | CodeQL/Trivy |

---

## Maintenance

| Activity | Frequency |
|----------|-----------|
| Review coverage gaps | Per PR |
| Update test builders | When entities change |
| Review architecture rules | Quarterly |
| Update critical rule tests | When rules change |
| Performance benchmarks | Per release |

---

*Document version: 1.0 | Last updated: 2026-08-16*