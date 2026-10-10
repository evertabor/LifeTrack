using LifeTrack.Application.Finance.Commands.ListCategories;

namespace LifeTrack.Web.ViewModels.Categories;

public sealed record CategoryListViewModel
{
    public CategoryFormViewModel NewCategory { get; init; } = new();
    public IReadOnlyList<CategoryFormViewModel> Categories { get; init; } = [];
    public string? SuccessMessage { get; init; }
    public string CategoryCountLabel { get; init; } = "0 categorías";
    public string CategoryCount { get; init; } = "0";

    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);
    public bool HasCategories => Categories.Count > 0;
    public bool IsEmpty => Categories.Count == 0;

    public static CategoryListViewModel From(IReadOnlyList<CategorySummary> categories, string? message) =>
        new()
        {
            Categories = categories.Select(CategoryFormViewModel.From).ToList(),
            SuccessMessage = message,
            CategoryCountLabel = $"{categories.Count} categorías",
            CategoryCount = categories.Count.ToString()
        };
}
