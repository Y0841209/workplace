using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Application.Features.Resources.Commands;
using WorkplaceBooking.Domain.Interfaces;

namespace WorkplaceBooking.Application.Features.Resources.Handlers;

public class DeleteResourceHandler : IRequestHandler<DeleteResourceCommand, Result>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteResourceHandler(
        IRepository<Resource> resourceRepository,
        IUnitOfWork unitOfWork)
    {
        _resourceRepository = resourceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
            return Result.NotFound("Resource not found");

        _resourceRepository.Delete(resource);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}