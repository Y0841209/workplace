using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Infrastructure.Services;

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
        // First check resource exists and is active/reservable
        var resource = await _resourceRepository.GetByIdAsync(new ResourceByIdSpec(resourceId), cancellationToken);
        if (resource == null || !resource.Active || !resource.Reservable)
            return false;

        // Check for overlapping reservations using exclusion constraint
        // The database exclusion constraint handles this, but we check for better UX
        var spec = new OverlappingReservationSpec(resourceId, date, startTime, endTime, excludeReservationId);
        var overlapping = await _reservationRepository.AnyAsync(spec, cancellationToken);
        return !overlapping;
    }
}