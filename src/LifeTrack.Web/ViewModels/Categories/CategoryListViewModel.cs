using LifeTrack.Application.Finance.Commands.ListCategories;

namespace LifeTrack.Web.ViewModels.Categories;

public sealed record CategoryListViewModel
{
    public CreateCategoryInputModel NewCategory { get; init; } = new();
    public IReadOnlyList<CategoryFormViewModel> Categories { get; init; } = [];
    public string? SuccessMessage { get; init; }
    public string CategoryCountLabel { get; init; } = "0 categorías";
    public string CategoryCount { get; init; } = "0";
    public string SearchTerm { get; init; } = string.Empty;
    public string NoResultsMessage { get; init; } = string.Empty;

    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);
    public bool HasCategories => Categories.Count > 0;
    public bool IsEmpty => Categories.Count == 0;
    public bool HasActiveSearch => !string.IsNullOrEmpty(SearchTerm);

    public static CategoryListViewModel From(
        IReadOnlyList<CategorySummary> categories,
        string? message,
        CreateCategoryInputModel? newCategory = null,
        string? searchTerm = null)
    {
        var trimmedSearchTerm = searchTerm?.Trim() ?? string.Empty;
        return new()
        {
            NewCategory = newCategory ?? new(),
            Categories = categories.Select(CategoryFormViewModel.From).ToList(),
            SuccessMessage = message,
            CategoryCountLabel = $"{categories.Count} categorías",
            CategoryCount = categories.Count.ToString(),
            SearchTerm = trimmedSearchTerm,
            NoResultsMessage = trimmedSearchTerm.Length == 0
                ? string.Empty
                : $"Sin resultados para «{trimmedSearchTerm}»."
        };
    }
}
