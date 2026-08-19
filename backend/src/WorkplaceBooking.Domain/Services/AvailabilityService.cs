using Ardalis.Specification;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;
using WorkplaceBooking.SharedKernel.Primitives;

namespace WorkplaceBooking.Domain.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IRepository<Resource> _resourceRepository;

    public AvailabilityService(
        IRepository<Reservation> reservationRepository,
        IRepository<Resource> resourceRepository)
    {
        _reservationRepository = reservationRepository;
        _resourceRepository = resourceRepository;
    }

    public async Task<bool> IsAvailableAsync(
        Guid resourceId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken cancellationToken = default,
        Guid? excludeReservationId = null)
    {
        // First check resource exists and is active
        var resource = await _resourceRepository.GetByIdAsync(resourceId, cancellationToken);
        if (resource == null || !resource.Active || !resource.Reservable)
            return false;

        // Check for overlapping reservations
        var spec = new OverlappingReservationSpec(resourceId, date, startTime, endTime, excludeReservationId);
        var overlapping = await _reservationRepository.AnyAsync(spec, cancellationToken);
        return !overlapping;
    }
}