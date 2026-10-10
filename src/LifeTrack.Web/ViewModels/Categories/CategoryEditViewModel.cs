using LifeTrack.Application.Finance.Commands.ListCategories;

namespace LifeTrack.Web.ViewModels.Categories;

public sealed record CategoryEditViewModel
{
    public Guid Id { get; init; }
    public string NewName { get; init; } = string.Empty;

    public static CategoryEditViewModel From(CategorySummary summary) =>
        new() { Id = summary.Id, NewName = summary.Name };
}
