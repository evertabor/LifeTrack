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

    public static TransactionEditViewModel From(
        TransactionSummary summary,
        IReadOnlyList<CategorySummary> categories) =>
        From(summary.Id, new EditTransactionInputModel
        {
            CategoryId = summary.CategoryId,
            Type = summary.IsIncome,
            Amount = summary.Amount,
            Date = summary.Date,
            Description = summary.Description
        }, categories);

    public static TransactionEditViewModel From(
        Guid id,
        EditTransactionInputModel input,
        IReadOnlyList<CategorySummary> categories) =>
        new()
        {
            Id = id,
            Input = input,
            CategoryOptions = categories
                .Select(c => TransactionCategoryOption.From(c, input.CategoryId))
                .ToList(),
            IsIncomeSelected = input.Type is true,
            IsExpenseSelected = input.Type is not true
        };
}
