using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.DeleteCategory;

public sealed class DeleteCategoryHandler(
    ICategoryRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede eliminar una categoría sin usuario autenticado.");
        }

        var category = await repository.GetByIdAsync(request.Id, currentUser.UserId.Value, ct);

        if (category is null)
        {
            throw new ResourceNotFoundException("La categoría no existe.");
        }

        await repository.RemoveAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
