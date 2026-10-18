using LifeTrack.Application.Finance.Commands.ListCategories;

namespace LifeTrack.Web.ViewModels.Categories;

public enum CategoryTypeFilter
{
    All = 0,
    Income = 1,
    Expense = 2
}

public sealed record CategoryListViewModel
{
    public CreateCategoryInputModel NewCategory { get; init; } = new();
    public IReadOnlyList<CategoryFormViewModel> Categories { get; init; } = [];
    public string? SuccessMessage { get; init; }
    public string CategoryCountLabel { get; init; } = "0 categorías";
    public string CategoryCount { get; init; } = "0";
    public string SearchTerm { get; init; } = string.Empty;
    public CategoryTypeFilter TypeFilter { get; init; }
    public string NoResultsMessage { get; init; } = string.Empty;

    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);
    public bool HasCategories => Categories.Count > 0;
    public bool IsEmpty => Categories.Count == 0;
    public bool HasActiveSearch => !string.IsNullOrEmpty(SearchTerm) || TypeFilter != CategoryTypeFilter.All;

    public static CategoryListViewModel From(
        IReadOnlyList<CategorySummary> categories,
        string? message,
        CreateCategoryInputModel? newCategory = null,
        string? searchTerm = null,
        CategoryTypeFilter typeFilter = CategoryTypeFilter.All)
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
            TypeFilter = typeFilter,
            NoResultsMessage = trimmedSearchTerm.Length == 0 && typeFilter == CategoryTypeFilter.All
                ? string.Empty
                : $"Sin resultados para «{trimmedSearchTerm}»{GetTypeFilterSuffix(typeFilter)}."
        };
    }

    private static string GetTypeFilterSuffix(CategoryTypeFilter filter)
    {
        return filter switch
        {
            CategoryTypeFilter.Income => " (solo ingresos)",
            CategoryTypeFilter.Expense => " (solo gastos)",
            _ => string.Empty
        };
    }
}
