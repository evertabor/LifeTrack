using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.CreateCategory;

public sealed class CreateCategoryHandler(
    ICategoryRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<CreateCategoryCommand, CategoryResult>
{
    public async Task<CategoryResult> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede crear una categoría sin usuario autenticado.");
        }
        try
        {
            var category = Category.Create(request.Name, currentUser.UserId.Value);
            if (await repository.ExistsAsync(category.Name, category.UserId, ct))
            {
                throw new DomainException("Ya existe una categoría con ese nombre.");
            }
            await repository.AddAsync(category, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return new CategoryResult(category.Id, category.Name);
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }
}
