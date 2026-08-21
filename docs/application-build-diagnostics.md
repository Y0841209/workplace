# Application Build Diagnostics

**Project**: `WorkplaceBooking.Application`
**Date**: 2026-08-19
**Total Errors**: 54

---

## Error Classification

### 1. Missing Using Statements (12 errors)

| File | Line | Type | Recommended Fix | Risk |
|------|------|------|-----------------|------|
| `Features/Resources/Handlers/CreateResourceHandler.cs` | 77, 80 | `Error` type not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Features/Resources/Handlers/UpdateResourceHandler.cs` | 93, 96 | `Error` type not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Features/Reservations/Handlers/CancelReservationHandler.cs` | 41 | `Error` type not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Features/Reservations/Handlers/UpdateReservationHandler.cs` | 49, 61, 64, 67 | `Error` type not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Features/Reservations/Handlers/CreateReservationHandler.cs` | 81 | `Error` type not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Features/Resources/Handlers/ImportResourcesHandler.cs` | 135 | `Error` type not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Features/Reservations/Mappings/ReservationMappingProfile.cs` | 19, 20 | `CheckInDto`, `AvailabilitySlot`, `AvailabilitySlotDto` not found | Add using for `Features.CheckIns.DTOs` and `Features.Resources.DTOs` | Low |
| `DependencyInjection.cs` | 31, 32, 33 | Validator types not found | Add `using WorkplaceBooking.Application.Validators;` | Low |

---

### 2. Missing References to Error or Result (14 errors)

| File | Line | Type | Recommended Fix | Risk |
|------|------|------|-----------------|------|
| `Features/Resources/Handlers/CreateResourceHandler.cs` | 77, 80 | `Error` not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Features/Resources/Handlers/CreateResourceHandler.cs` | 95 | `Result<Resource>.Errors` not found | Fix: `Result<Resource>` uses custom `Result` type, not Ardalis.Result. Use `result.IsSuccess` pattern instead of `result.Errors` | Medium |
| `Features/Resources/Handlers/UpdateResourceHandler.cs` | 93, 96, 61, 64, 67 | `Error` not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Features/Reservations/Handlers/CancelReservationHandler.cs` | 41 | `Error` not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Features/Reservations/Handlers/CancelReservationHandler.cs` | 48 | `Result.Errors` not found | Use custom Result pattern: check `IsSuccess`/`IsFailure` | Medium |
| `Features/Reservations/Handlers/UpdateReservationHandler.cs` | 49, 61, 64, 67 | `Error` not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Features/Reservations/Handlers/UpdateReservationHandler.cs` | 94 | `Result.Errors` not found | Use custom Result pattern | Medium |
| `Features/Reservations/Handlers/CreateReservationHandler.cs` | 81 | `Error` not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Features/Reservations/Handlers/CreateReservationHandler.cs` | 99 | `Result<Reservation>.Errors` not found | Use custom Result pattern | Medium |
| `Features/Reservations/Handlers/CheckInReservationHandler.cs` | 86, 94 | `Result<T>.Errors` not found | Use custom Result pattern | Medium |
| `Features/Resources/Handlers/ImportResourcesHandler.cs` | 118 | `Result<Resource>.Errors` not found | Use custom Result pattern | Medium |
| `Features/Resources/Handlers/ImportResourcesHandler.cs` | 135 | `Error` not found | Add `using WorkplaceBooking.SharedKernel.Results;` | Low |
| `Validators/ResourceValidators.cs` | 101 | Validator type mismatch | Fix RuleForEach to use correct validator type | Medium |

---

### 3. Missing Extension Methods (8 errors)

| File | Line | Type | Recommended Fix | Risk |
|------|------|------|-----------------|------|
| `Features/CheckIns/Handlers/GetCheckInHistoryHandler.cs` | 32 | `CheckInsByUserSpec.WithPaging` missing | Add extension method `WithPaging` to `Specification<T>` in Domain.Specifications or add to `Ardalis.Specification` extensions | Medium |
| `Features/Reservations/Handlers/GetMyReservationsHandler.cs` | 37 | `MyReservationsSpec.WithPaging` missing | Same as above | Medium |
| `Features/Resources/Handlers/GetResourcesHandler.cs` | 46 | `ResourcesFilteredSpec.WithPaging` missing | Same as above | Medium |
| `Features/Resources/Handlers/GetResourcesByFloorHandler.cs` | (implied) | `ResourcesByFloorSpec.WithPaging` missing | Same as above | Medium |
| `Features/Reservations/Validators/CreateReservationValidator.cs` | 67 | `HasActiveExceptionAsync` expects string, gets Guid | Fix parameter type: pass `resource.ResourceTypeCode` (string) instead of `command.ResourceId` (Guid) | Low |

---

### 4. Missing WithPaging Implementation (5 errors)

| File | Line | Type | Recommended Fix | Risk |
|------|------|------|-----------------|------|
| `Features/CheckIns/Handlers/GetCheckInHistoryHandler.cs` | 32 | `CheckInsByUserSpec` no `WithPaging` | Implement `WithPaging` extension in Domain.Specifications | Medium |
| `Features/Reservations/Handlers/GetMyReservationsHandler.cs` | 37 | `MyReservationsSpec` no `WithPaging` | Same | Medium |
| `Features/Resources/Handlers/GetResourcesHandler.cs` | 46 | `ResourcesFilteredSpec` no `WithPaging` | Same | Medium |
| `Features/Resources/Handlers/GetResourcesByFloorHandler.cs` | (implied) | `ResourcesByFloorSpec` no `WithPaging` | Same | Medium |
| `Domain/Specifications/*.cs` | N/A | Missing extension | Add to Domain project: `public static Specification<T> WithPaging(this Specification<T> spec, int page, int pageSize) => spec.Skip((page-1)*pageSize).Take(pageSize);` | Medium |

---

### 5. Namespace Mismatches (6 errors)

| File | Line | Type | Recommended Fix | Risk |
|------|------|------|-----------------|------|
| `Features/Resources/Handlers/RegenerateResourceQrHandler.cs` | 31, 33, 36 | Self-assignment (`x = x`) | Fix constructor: remove duplicate assignments; ensure fields initialized from parameters | High |
| `Features/Resources/Handlers/RegenerateResourceQrHandler.cs` | 21 | Non-nullable fields not initialized | Add `required` modifier or initialize in constructor | Medium |
| `Features/Resources/Handlers/GetResourcesByFloorHandler.cs` | 47 | Variable `floor` declared twice | Rename inner variable (e.g., `var floorEntity`) | Low |
| `Features/Resources/Handlers/GetAvailabilityHandler.cs` | 70 | `List<T>` to `IReadOnlyList<T>` implicit conversion | Change return type or cast: `return Result.Success<IReadOnlyList<...>>(slots);` | Low |
| `Features/Resources/Handlers/GetMeetingRoomsHandler.cs` | 41 | Passing string to Guid parameter | Fix: `GetByIdAsync(resourceType.Id)` not `GetByIdAsync(resourceType.Code)` | Medium |
| `Features/Resources/Handlers/GetResourceByIdHandler.cs` | 40 | Passing string to Guid parameter | Fix: `GetByIdAsync(resource.ResourceTypeCode)` should use `resource.ResourceTypeId` if Guid | Medium |

---

### 6. Handler Implementation Bugs (8 errors)

| File | Line | Type | Recommended Fix | Risk |
|------|------|------|-----------------|------|
| `Features/Resources/Handlers/RegenerateResourceQrHandler.cs` | 31, 33, 36 | Self-assignment `_field = _field` | Fix constructor parameter names and assignments | High |
| `Features/Resources/Handlers/RegenerateResourceQrHandler.cs` | 21 | Fields `_resourceTypeRepository`, `_floorRepository`, `_mapper` not initialized | Initialize from constructor parameters | High |
| `Features/Resources/Handlers/RegenerateResourceQrHandler.cs` | 51 | Passing string to Guid parameter | Fix: `GetByIdAsync(resource.ResourceTypeCode)` → should be `resource.ResourceTypeId` if exists | Medium |
| `Features/Resources/Handlers/UpdateResourceHandler.cs` | 99 | `void` assigned to implicit variable | Remove `var` from `resource.Update(...)` call | Low |
| `Features/Resources/Handlers/GetResourcesByFloorHandler.cs` | 47 | Variable `floor` declared twice | Rename inner variable | Low |
| `Features/Resources/Handlers/GetAvailabilityHandler.cs` | 70 | Type mismatch `List` vs `IReadOnlyList` | Cast or change return type | Low |
| `Validators/ResourceValidators.cs` | 101 | `RuleForEach` type mismatch | Use `.SetValidator` with correct generic types | Medium |
| `Validators/CreateReservationValidator.cs` | 67 | Passing Guid to string parameter | Fix: pass `resource.ResourceTypeCode` instead of `command.ResourceId` | Low |

---

### 7. Validator Bugs (3 errors)

| File | Line | Type | Recommended Fix | Risk |
|------|------|------|-----------------|------|
| `Validators/ResourceValidators.cs` | 101 | `RuleForEach` validator type mismatch | Change to: `RuleForEach(x => x.Resources).SetValidator(new CreateResourceDtoValidator())` | Medium |
| `Validators/CreateReservationValidator.cs` | 67 | `HasActiveExceptionAsync` parameter mismatch | Pass `resourceTypeCode` (string) not `ResourceId` (Guid) | Low |
| `DependencyInjection.cs` | 31-33 | Missing `AddValidatorsFromAssemblyContaining` | Install `FluentValidation.DependencyInjectionExtensions` package or fix using | Medium |

---

### 8. DTO Mapping Bugs (3 errors)

| File | Line | Type | Recommended Fix | Risk |
|------|------|------|-----------------|------|
| `Features/Reservations/Mappings/ReservationMappingProfile.cs` | 19 | `CheckInDto` not found | Add `using WorkplaceBooking.Application.Features.CheckIns.DTOs;` | Low |
| `Features/Reservations/Mappings/ReservationMappingProfile.cs` | 20 | `AvailabilitySlot` not found | Add `using WorkplaceBooking.Application.Features.Resources.DTOs;` | Low |
| `Features/Reservations/Mappings/ReservationMappingProfile.cs` | 20 | `AvailabilitySlotDto` not found | Same as above | Low |

---

### 9. MediatR Request/Response Type Errors (6 errors)

| File | Line | Type | Recommended Fix | Risk |
|------|------|------|-----------------|------|
| `Features/Reservations/Handlers/CreateReservationHandler.cs` | (implied) | Type mismatch in `IRequestHandler` | Verify command implements `IRequest<Ardalis.Result.Result<ReservationDto>>` | Low |
| `Features/Resources/Handlers/CreateResourceHandler.cs` | (implied) | Type mismatch in `IRequestHandler` | Verify command implements `IRequest<Ardalis.Result.Result<ResourceDto>>` | Low |
| `Features/Resources/Handlers/RegenerateResourceQrHandler.cs` | (implied) | Type mismatch in `IRequestHandler` | Verify command implements `IRequest<Ardalis.Result.Result<ResourceDto>>` | Low |
| `Features/Resources/Handlers/UpdateResourceHandler.cs` | (implied) | Type mismatch in `IRequestHandler` | Verify command implements `IRequest<Ardalis.Result.Result<ResourceDto>>` | Low |
| `Features/CheckIns/Handlers/GetCheckInHistoryHandler.cs` | 12 | Return type mismatch `PagedResult` | Use fully qualified `WorkplaceBooking.Application.Common.DTOs.PagedResult` | Low |
| `Features/Reservations/Handlers/GetMyReservationsHandler.cs` | 13 | Return type mismatch `PagedResult` | Use fully qualified `WorkplaceBooking.Application.Common.DTOs.PagedResult` | Low |

---

### 10. Dependency Injection Issues (8 errors)

| File | Line | Type | Recommended Fix | Risk |
|------|------|------|-----------------|------|
| `DependencyInjection.cs` | 31 | `CreateResourceValidator` not found | Add `using WorkplaceBooking.Application.Validators;` | Low |
| `DependencyInjection.cs` | 32 | `CreateReservationValidator` not found | Add `using WorkplaceBooking.Application.Validators;` | Low |
| `DependencyInjection.cs` | 33 | `CreateResourceDtoValidator` not found | Fix: validator class name is `CreateResourceValidator` | Low |
| `DependencyInjection.cs` | 31-33 | `AddValidatorsFromAssemblyContaining` missing | Install `FluentValidation.DependencyInjectionExtensions` NuGet package | Medium |
| `Common/Behaviors/TransactionBehavior.cs` | 27 | `Microsoft.EntityFrameworkCore` not found | Install `Microsoft.EntityFrameworkCore` NuGet package or remove EF-specific exception handling | Medium |
| `Common/Behaviors/TransactionBehavior.cs` | 27 | `DbUpdateConcurrencyException` not found | Same as above | Medium |
| `Common/Behaviors/TransactionBehavior.cs` | 27 | `DbUpdateException` not found | Same as above | Medium |
| `Common/Behaviors/ValidationBehavior.cs` | (implied) | May need fixes | Verify implementation matches MediatR v12 | Low |

---

## Priority Fix Order

1. **Critical** (build blockers): Namespace mismatches in RegenerateResourceQrHandler, missing NuGet packages
2. **High** (pattern issues): Result/Error usage across handlers, WithPaging extensions
3. **Medium** (missing using/references): Add using statements, fix DTO mappings
4. **Low** (cleanup): Variable naming, type conversions

---

## Notes

- The **Domain** and **SharedKernel** projects compile successfully (validated separately)
- The custom `Result` pattern in `SharedKernel` differs from `Ardalis.Result` - handlers mixing both cause errors
- **WithPaging** extension method needs to be added to Domain.Specifications project
- NuGet packages needed: `FluentValidation.DependencyInjectionExtensions`, `Microsoft.EntityFrameworkCore`