using Ardalis.Specification;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Domain.Specifications;

public class FutureActiveReservationsSpec : Specification<Reservation>
{
    public FutureActiveReservationsSpec(Guid userId)
    {
        Query.Where(r => r.UserId == userId
            && r.Status == ReservationStatus.CONFIRMED
            && r.ReservationDate >= DateOnly.FromDateTime(DateTime.Today));
    }
}

public class MyReservationsSpec : Specification<Reservation>
{
    public MyReservationsSpec(Guid userId, string? status = null, DateOnly? dateFrom = null, DateOnly? dateTo = null)
    {
        Query.Where(r => r.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReservationStatus>(status, true, out var parsedStatus))
            Query.Where(r => r.Status == parsedStatus);

        if (dateFrom.HasValue)
            Query.Where(r => r.ReservationDate >= dateFrom.Value);

        if (dateTo.HasValue)
            Query.Where(r => r.ReservationDate <= dateTo.Value);

        Query.OrderByDescending(r => r.ReservationDate)
             .ThenByDescending(r => r.StartTime);
    }
}

public class ResourceByPublicQrSpec : Specification<Resource>
{
    public ResourceByPublicQrSpec(Guid publicQrId)
    {
        Query.Where(r => r.PublicQrId == publicQrId && r.Active && r.Reservable);
    }
}

public class ActiveReservationForResourceSpec : Specification<Reservation>
{
    public ActiveReservationForResourceSpec(Guid resourceId, DateOnly date)
    {
        Query.Where(r => r.ResourceId == resourceId
            && r.ReservationDate == date
            && r.Status == ReservationStatus.CONFIRMED);
    }
}

public class ActiveReservationForUserSpec : Specification<Reservation>
{
    public ActiveReservationForUserSpec(Guid userId, DateOnly date)
    {
        Query.Where(r => r.UserId == userId
            && r.ReservationDate == date
            && r.Status == ReservationStatus.CONFIRMED);
    }
}

public class ReservationByIdSpec : Specification<Reservation>
{
    public ReservationByIdSpec(Guid reservationId)
    {
        Query.Where(r => r.Id == reservationId);
    }
}

public class UserBusinessProfileSpec : Specification<UserBusinessProfile>
{
    public UserBusinessProfileSpec(Guid userId, bool onlyActive = true)
    {
        Query.Where(u => u.UserId == userId);
        if (onlyActive)
            Query.Where(u => u.Active && DateOnly.FromDateTime(DateTime.Today) >= u.ValidFrom &&
                            (!u.ExpiresAt.HasValue || DateOnly.FromDateTime(DateTime.Today) <= u.ExpiresAt.Value));
    }
}

public class UserApplicationRoleSpec : Specification<UserApplicationRole>
{
    public UserApplicationRoleSpec(Guid userId, bool onlyActive = true)
    {
        Query.Where(u => u.UserId == userId);
        if (onlyActive)
            Query.Where(u => u.Active && DateOnly.FromDateTime(DateTime.Today) >= u.ValidFrom &&
                            (!u.ExpiresAt.HasValue || DateOnly.FromDateTime(DateTime.Today) <= u.ExpiresAt.Value));
    }
}

public class CheckInsByUserSpec : Specification<CheckIn>
{
    public CheckInsByUserSpec(Guid userId, DateOnly? dateFrom = null, DateOnly? dateTo = null)
    {
        Query.Where(c => c.UserId == userId);

        if (dateFrom.HasValue)
            Query.Where(c => c.CheckedInAt.Date >= dateFrom.Value.ToDateTime(TimeOnly.MinValue));

        if (dateTo.HasValue)
            Query.Where(c => c.CheckedInAt.Date <= dateTo.Value.ToDateTime(TimeOnly.MaxValue));

        Query.OrderByDescending(c => c.CheckedInAt);
    }
}

public class CheckInsByResourceSpec : Specification<CheckIn>
{
    public CheckInsByResourceSpec(Guid resourceId, DateOnly? date = null)
    {
        Query.Where(c => c.ResourceId == resourceId);

        if (date.HasValue)
            Query.Where(c => c.CheckedInAt.Date == date.Value.ToDateTime(TimeOnly.MinValue));

        Query.OrderByDescending(c => c.CheckedInAt);
    }
}

public class CheckInsByReservationsSpec : Specification<CheckIn>
{
    public CheckInsByReservationsSpec(IEnumerable<Guid> reservationIds)
    {
        var ids = reservationIds.ToList();
        Query.Where(c => ids.Contains(c.ReservationId));
        Query.OrderByDescending(c => c.CheckedInAt);
    }
}

public class CheckInsByResourceAndDateSpec : Specification<CheckIn>
{
    public CheckInsByResourceAndDateSpec(Guid resourceId, DateOnly date)
    {
        Query.Where(c => c.ResourceId == resourceId
            && c.CheckedInAt.Date == date.ToDateTime(TimeOnly.MinValue));
    }
}