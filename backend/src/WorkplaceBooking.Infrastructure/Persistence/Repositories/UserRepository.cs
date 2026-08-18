using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Infrastructure.Persistence.Repositories;

public class UserRepository : IRepository<AppUser>
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<AppUser?> FirstOrDefaultAsync(ISpecification<AppUser> spec, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(spec);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AppUser>> ListAsync(ISpecification<AppUser> spec, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(spec);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(ISpecification<AppUser> spec, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(spec);
        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(AppUser entity, CancellationToken cancellationToken = default)
    {
        await _context.AppUsers.AddAsync(entity, cancellationToken);
    }

    public void Update(AppUser entity)
    {
        _context.AppUsers.Update(entity);
    }

    public void Delete(AppUser entity)
    {
        _context.AppUsers.Remove(entity);
    }

    public async Task<AppUser?> GetByEntraIdAsync(Guid entraObjectId, CancellationToken cancellationToken = default)
    {
        return await _context.AppUsers
            .FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, cancellationToken);
    }

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    private IQueryable<AppUser> ApplySpecification(ISpecification<AppUser> spec)
    {
        var evaluator = new SpecificationEvaluator<AppUser>();
        return evaluator.GetQuery(_context.AppUsers.AsQueryable(), spec);
    }
}