using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.EditCategory;

public sealed class EditCategoryHandler(
    ICategoryRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<EditCategoryCommand>
{
    public async Task Handle(EditCategoryCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede editar una categoría sin usuario autenticado.");
        }

        var category = await repository.GetByIdAsync(request.Id, currentUser.UserId.Value, ct);

        if (category is null)
        {
            throw new ResourceNotFoundException("La categoría no existe.");
        }

        try
        {
            var normalizedName = request.Name?.Trim() ?? string.Empty;

            if (normalizedName != category.Name &&
                await repository.ExistsAsync(normalizedName, category.UserId, ct))
            {
                throw new DomainException("Ya existe una categoría con ese nombre.");
            }

            category.EditName(request.Name);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }
}
