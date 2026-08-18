namespace WorkplaceBooking.SharedKernel.Primitives;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
    Guid EventId { get; }
}