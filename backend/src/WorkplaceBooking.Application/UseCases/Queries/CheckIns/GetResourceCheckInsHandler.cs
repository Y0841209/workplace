using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.UseCases.Queries.CheckIns;

public class GetResourceCheckInsHandler : IRequestHandler<GetResourceCheckInsQuery, Result<IReadOnlyList<CheckInDto>>>
{
    private readonly IRepository<CheckIn> _checkInRepository;
    private readonly IRepository<Resource> _resourceRepository;

    public GetResourceCheckInsHandler(
        IRepository<CheckIn> checkInRepository,
        IRepository<Resource> resourceRepository)
    {
        _checkInRepository = checkInRepository;
        _resourceRepository = resourceRepository;
    }

    public async Task<Result<IReadOnlyList<CheckInDto>>> Handle(GetResourceCheckInsQuery request, CancellationToken cancellationToken)
    {
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
            return Result.NotFound("Resource not found");

        // Only allow check-in queries for office types
        if (resource.ResourceTypeCode == "MEETING_ROOM")
            return Result.Error("Check-in history not available for meeting rooms");

        var spec = new CheckInsByResourceSpec(resource.Id, request.Date);
        var checkIns = await _checkInRepository.ListAsync(spec, cancellationToken);

        var items = checkIns.Select(c => new CheckInDto(
            c.Id,
            c.ReservationId,
            c.ResourceId,
            c.UserId,
            c.CheckedInAt,
            c.IpAddress,
            c.UserAgent)).ToList();

        return Result.Success<IReadOnlyList<CheckInDto>>(items);
    }
}