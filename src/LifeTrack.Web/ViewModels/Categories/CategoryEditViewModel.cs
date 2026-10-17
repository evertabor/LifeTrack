using LifeTrack.Application.Finance.Commands.ListCategories;

namespace LifeTrack.Web.ViewModels.Categories;

public sealed record CategoryEditViewModel
{
    public Guid Id { get; init; }
    public string NewName { get; init; } = string.Empty;
    public bool? Type { get; init; }
    public string TypeHelpId { get; init; } = string.Empty;

    public static CategoryEditViewModel From(CategorySummary summary) =>
        From(summary.Id, summary.Name, summary.IsIncome);

    public static CategoryEditViewModel From(Guid id, string name, bool isIncome) =>
        new()
        {
            Id = id,
            NewName = name,
            Type = isIncome,
            TypeHelpId = $"category-type-help-{id:D}"
        };
}
