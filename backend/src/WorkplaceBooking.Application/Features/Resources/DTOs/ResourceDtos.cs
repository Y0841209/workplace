using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.Features.Resources.DTOs;

public record ResourceDto(
    Guid Id,
    string Code,
    string Name,
    string ResourceTypeCode,
    string ResourceTypeName,
    Guid LocationId,
    string LocationName,
    Guid FloorId,
    string FloorCode,
    Guid? ZoneId,
    string? ZoneName,
    int Capacity,
    Guid? PublicQrId,
    int QrVersion,
    bool Active,
    bool Reservable,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ResourceTypeDto(
    string Code,
    string Name,
    bool QrRequired,
    bool CheckinRequired,
    bool Active);

public record AvailabilitySlotDto(
    Guid ResourceId,
    string ResourceCode,
    string ResourceName,
    string ResourceTypeCode,
    Guid FloorId,
    string FloorName,
    Guid? ZoneId,
    string? ZoneName,
    int Capacity,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool Available);

public record ResourcesByFloorDto(
    Guid FloorId,
    string FloorName,
    int FloorNumber,
    IReadOnlyList<ResourceDto> Resources);