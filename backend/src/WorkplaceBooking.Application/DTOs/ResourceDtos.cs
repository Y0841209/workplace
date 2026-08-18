namespace WorkplaceBooking.Application.DTOs;

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