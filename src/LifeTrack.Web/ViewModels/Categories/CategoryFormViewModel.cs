using LifeTrack.Application.Finance.Commands.ListCategories;

namespace LifeTrack.Web.ViewModels.Categories;

public sealed record CategoryFormViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public static CategoryFormViewModel From(CategorySummary summary) =>
        new() { Id = summary.Id, Name = summary.Name };
}
