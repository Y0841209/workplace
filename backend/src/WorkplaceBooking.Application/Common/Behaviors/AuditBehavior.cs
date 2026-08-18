using MediatR;
using Microsoft.Extensions.Logging;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.Common.Behaviors;

public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUser;

    public AuditBehavior(ILogger<AuditBehavior<TRequest, TResponse>> logger, ICurrentUserService currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (IsAuditRequired(request))
        {
            await LogAuditAsync(request, response);
        }

        return response;
    }

    private bool IsAuditRequired(TRequest request)
    {
        var requestName = typeof(TRequest).Name;
        return requestName.EndsWith("Command") || 
               requestName.Contains("Create") || 
               requestName.Contains("Update") || 
               requestName.Contains("Delete") ||
               requestName.Contains("Assign") ||
               requestName.Contains("Assign") ||
               requestName.Contains("Cancel") ||
               requestName.Contains("CheckIn") ||
               requestName.Contains("CheckOut");
    }

    private Task LogAuditAsync(TRequest request, TResponse response)
    {
        // In a real implementation, this would use an IAuditService
        // For now, we just log the audit information
        var userId = _currentUser.UserId;
        var action = typeof(TRequest).Name.Replace("Command", "");
        
        _logger.LogInformation(
            "Audit: {Action} performed by User {UserId} at {Timestamp}",
            action, userId, DateTimeOffset.UtcNow);

        return Task.CompletedTask;
    }
}