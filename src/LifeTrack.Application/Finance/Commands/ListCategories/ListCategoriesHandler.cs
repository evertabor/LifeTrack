using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.ListCategories;

public sealed class ListCategoriesHandler(
    ICategoryRepository repository,
    ICurrentUser currentUser) : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategorySummary>>
{
    public async Task<IReadOnlyList<CategorySummary>> Handle(ListCategoriesQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede listar categorías sin usuario autenticado.");
        }
        var items = await repository.ListAsync(currentUser.UserId.Value, ct);
        return items.Select(c => new CategorySummary(c.Id, c.Name)).ToList();
    }
}
