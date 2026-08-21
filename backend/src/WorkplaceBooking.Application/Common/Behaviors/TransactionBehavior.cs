using MediatR;
using Microsoft.Extensions.Logging;
using WorkplaceBooking.Domain.Interfaces;

namespace WorkplaceBooking.Application.Common.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IUnitOfWork unitOfWork, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex) || IsDatabaseUpdateException(ex))
        {
            _logger.LogError(ex, "Database error while saving changes");
            throw new InvalidOperationException("An error occurred while saving the data. Please try again.", ex);
        }

        return response;
    }

    private static bool IsConcurrencyException(Exception ex)
    {
        return ex.GetType().Name.Contains("DbUpdateConcurrencyException") ||
               (ex.InnerException != null && IsConcurrencyException(ex.InnerException));
    }

    private static bool IsDatabaseUpdateException(Exception ex)
    {
        return ex.GetType().Name.Contains("DbUpdateException") ||
               (ex.InnerException != null && IsDatabaseUpdateException(ex.InnerException));
    }
}