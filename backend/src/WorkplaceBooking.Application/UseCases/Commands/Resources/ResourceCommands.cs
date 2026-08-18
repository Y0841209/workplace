using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;

namespace WorkplaceBooking.Application.UseCases.Commands.Resources;

public record CreateResourceCommand(
    string Code,
    string Name,
    string ResourceTypeCode,
    Guid LocationId,
    Guid FloorId,
    Guid? ZoneId,
    int Capacity,
    Guid? PublicQrId = null) : IRequest<Result<ResourceDto>>;

public record UpdateResourceCommand(
    Guid ResourceId,
    string? Name = null,
    string? ResourceTypeCode = null,
    Guid? LocationId = null,
    Guid? FloorId = null,
    Guid? ZoneId = null,
    int? Capacity = null,
    Guid? PublicQrId = null,
    bool? Active = null,
    bool? Reservable = null) : IRequest<Result<ResourceDto>>;

public record DeleteResourceCommand(
    Guid ResourceId) : IRequest<Result>;

public record RegenerateResourceQrCommand(
    Guid ResourceId) : IRequest<Result<ResourceDto>>;

public record ImportResourcesCommand(
    List<CreateResourceCommand> Resources) : IRequest<Result<int>>;