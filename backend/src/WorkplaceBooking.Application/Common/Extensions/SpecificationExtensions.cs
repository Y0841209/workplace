using Ardalis.Specification;
using System.Linq;

namespace WorkplaceBooking.Application.Common.Extensions;

public static class SpecificationExtensions
{
    public static ISpecification<T> WithPaging<T>(
        this ISpecification<T> specification,
        int page,
        int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var skip = (page - 1) * pageSize;
        
        specification.Query.Skip(skip).Take(pageSize);
        
        return specification;
    }

    public static IQueryable<T> WithPaging<T>(
        this IQueryable<T> query,
        int page,
        int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var skip = (page - 1) * pageSize;
        
        return query.Skip(skip).Take(pageSize);
    }
}