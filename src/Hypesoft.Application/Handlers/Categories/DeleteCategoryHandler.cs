using MediatR;
using Hypesoft.Application.Commands.Categories;
using Hypesoft.Domain.Entities;
using Hypesoft.Domain.Exceptions;
using Hypesoft.Domain.Repositories;

namespace Hypesoft.Application.Handlers.Categories;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly ICategoryRepository _repository;

    public DeleteCategoryHandler(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        _ = await _repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Category), request.Id);

        await _repository.DeleteAsync(request.Id);
    }
}
