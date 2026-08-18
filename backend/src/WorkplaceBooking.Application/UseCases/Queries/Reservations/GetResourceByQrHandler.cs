using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Application.Interfaces;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.UseCases.Queries.Reservations;

public class GetResourceByQrHandler : IRequestHandler<GetResourceByQrQuery, Result<AvailabilitySlotDto>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IAvailabilityService _availabilityService;

    public GetResourceByQrHandler(
        IRepository<Resource> resourceRepository,
        IRepository<Reservation> reservationRepository,
        IAvailabilityService availabilityService)
    {
        _resourceRepository = resourceRepository;
        _reservationRepository = reservationRepository;
        _availabilityService = availabilityService;
    }

    public async Task<Result<AvailabilitySlotDto>> Handle(GetResourceByQrQuery request, CancellationToken cancellationToken)
    {
        // Find resource by public QR ID
        var resource = await _resourceRepository.FirstOrDefaultAsync(
            new ResourceByPublicQrSpec(request.PublicQrId), cancellationToken);

        if (resource == null)
            return Result.NotFound("Resource not found");

        // Check if resource type supports check-in
        if (resource.ResourceTypeCode != "OPEN_WORKSPACE" && resource.ResourceTypeCode != "CLOSED_OFFICE")
            return Result.Error("Check-in not available for this resource type");

        // Check if there's an active reservation for today
        var today = DateOnly.FromDateTime(DateTime.Today);
        var spec = new ActiveReservationForResourceSpec(resource.Id, today);
        var reservation = await _reservationRepository.FirstOrDefaultAsync(spec, CancellationToken.None);

        var available = reservation == null || reservation.Status != ReservationStatus.CONFIRMED;

        return Result.Success(new AvailabilitySlotDto(
            resource.Id,
            resource.Code,
            resource.Name,
            resource.ResourceTypeCode,
            resource.FloorId,
            "Piso",
            resource.ZoneId,
            null,
            resource.Capacity,
            TimeOnly.MinValue,
            TimeOnly.MaxValue,
            available));
    }
}