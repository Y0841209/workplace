using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Infrastructure.Persistence.Repositories;

public class ReservationRepository : IRepository<Reservation>
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Reservation?> FirstOrDefaultAsync(ISpecification<Reservation> spec, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(spec);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> ListAsync(ISpecification<Reservation> spec, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(spec);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(ISpecification<Reservation> spec, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(spec);
        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Reservation entity, CancellationToken cancellationToken = default)
    {
        await _context.Reservations.AddAsync(entity, cancellationToken);
    }

    public void Update(Reservation entity)
    {
        _context.Reservations.Update(entity);
    }

    public void Delete(Reservation entity)
    {
        _context.Reservations.Remove(entity);
    }

    private IQueryable<Reservation> ApplySpecification(ISpecification<Reservation> spec)
    {
        var evaluator = new SpecificationEvaluator<Reservation>();
        return evaluator.GetQuery(_context.Reservations.AsQueryable(), spec);
    }
}