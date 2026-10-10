using LifeTrack.Application.Finance.Commands.ListCategories;

namespace LifeTrack.Web.ViewModels.Categories;

public sealed record CategoryListViewModel
{
    public IReadOnlyList<CategoryFormViewModel> Categories { get; init; } = [];
    public string? SuccessMessage { get; init; }

    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);
    public bool HasCategories => Categories.Count > 0;
    public bool IsEmpty => Categories.Count == 0;

    public static CategoryListViewModel From(IReadOnlyList<CategorySummary> categories, string? message) =>
        new()
        {
            Categories = categories.Select(CategoryFormViewModel.From).ToList(),
            SuccessMessage = message
        };
}
