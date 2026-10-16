using LifeTrack.Application.Finance.Commands.ListCategories;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;

namespace LifeTrack.Web.ViewModels.Transactions;

public sealed record TransactionEditViewModel
{
    public Guid Id { get; init; }
    public EditTransactionInputModel Input { get; init; } = new();
    public IReadOnlyList<TransactionCategoryOption> CategoryOptions { get; init; } = [];
    public bool IsIncomeSelected { get; init; }
    public bool IsExpenseSelected { get; init; } = true;
    public string MinDateText { get; init; } = string.Empty;
    public string MaxDateText { get; init; } = string.Empty;

    public static TransactionEditViewModel From(
        TransactionSummary summary,
        IReadOnlyList<CategorySummary> categories,
        DateOnly? today = null) =>
        From(summary.Id, new EditTransactionInputModel
        {
            CategoryId = summary.CategoryId,
            Type = summary.IsIncome,
            Amount = summary.Amount,
            Date = summary.Date,
            Description = summary.Description
        }, categories, today ?? summary.Date);

    public static TransactionEditViewModel From(
        Guid id,
        EditTransactionInputModel input,
        IReadOnlyList<CategorySummary> categories,
        DateOnly? today = null)
    {
        var reference = input.Date ?? today ?? new DateOnly(2100, 12, 31);
        var monthStart = new DateOnly(reference.Year, reference.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var ceiling = today ?? reference;
        var maxDay = monthEnd <= ceiling ? monthEnd : ceiling;
        return new()
        {
            Id = id,
            Input = input,
            CategoryOptions = categories
                .Select(c => TransactionCategoryOption.From(c, input.CategoryId))
                .ToList(),
            IsIncomeSelected = input.Type is true,
            IsExpenseSelected = input.Type is not true,
            MinDateText = input.Date is null ? string.Empty : monthStart.ToString("yyyy-MM-dd"),
            MaxDateText = maxDay.ToString("yyyy-MM-dd")
        };
    }
}
