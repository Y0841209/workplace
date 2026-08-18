namespace WorkplaceBooking.SharedKernel.Primitives;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity() { }

    protected Entity(Guid id)
    {
        Id = id;
    }
}

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot() { }

    protected AggregateRoot(Guid id) : base(id) { }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}