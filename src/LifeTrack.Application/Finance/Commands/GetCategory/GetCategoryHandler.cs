using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.ListCategories;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.GetCategory;

public sealed class GetCategoryHandler(
    ICategoryRepository repository,
    ICurrentUser currentUser) : IRequestHandler<GetCategoryQuery, CategorySummary>
{
    public async Task<CategorySummary> Handle(GetCategoryQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede consultar una categoría sin usuario autenticado.");
        }

        var category = await repository.GetByIdAsync(request.Id, currentUser.UserId.Value, ct);

        if (category is null)
        {
            throw new ResourceNotFoundException("La categoría no existe.");
        }

        return new CategorySummary(category.Id, category.Name, category.Type == TransactionType.Income);
    }
}
