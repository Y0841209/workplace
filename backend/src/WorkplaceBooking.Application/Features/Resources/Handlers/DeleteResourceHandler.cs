using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Features.Resources.Commands;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;

namespace WorkplaceBooking.Application.Features.Resources.Handlers;

public class DeleteResourceHandler : IRequestHandler<DeleteResourceCommand, Ardalis.Result.Result>
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

    public async Task<Ardalis.Result.Result> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
            return Ardalis.Result.Result.NotFound("Resource not found");

        _resourceRepository.Delete(resource);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ardalis.Result.Result.Success();
    }
}