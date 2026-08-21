using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Infrastructure.Persistence.Repositories;

public class ResourceRepository : IRepository<Resource>
{
    private readonly AppDbContext _context;

    public ResourceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Resource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Resources
            .Include(r => r.ResourceType)
            .Include(r => r.Location)
            .Include(r => r.Floor)
            .Include(r => r.Zone)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Resource?> FirstOrDefaultAsync(ISpecification<Resource> spec, CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.Default.GetQuery(_context.Resources.AsQueryable(), spec);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Resource>> ListAsync(ISpecification<Resource> spec, CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.Default.GetQuery(_context.Resources.AsQueryable(), spec);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(ISpecification<Resource> spec, CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.Default.GetQuery(_context.Resources.AsQueryable(), spec);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(ISpecification<Resource> spec, CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.Default.GetQuery(_context.Resources.AsQueryable(), spec);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Resource entity, CancellationToken cancellationToken = default)
    {
        await _context.Resources.AddAsync(entity, cancellationToken);
    }

    public void Update(Resource entity)
    {
        _context.Resources.Update(entity);
    }

    public void Delete(Resource entity)
    {
        _context.Resources.Remove(entity);
    }
}