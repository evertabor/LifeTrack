using System.Globalization;
using LifeTrack.Application.Finance.Commands.GetBudgetStatus;
using LifeTrack.Application.Finance.Commands.ListCategories;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;

namespace LifeTrack.Web.ViewModels.Transactions;

public sealed record TransactionCategoryOption
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsSelected { get; init; }

    public static TransactionCategoryOption From(CategorySummary summary, Guid? selectedId) =>
        new()
        {
            Id = summary.Id,
            Name = summary.Name,
            IsSelected = selectedId.HasValue && selectedId.Value == summary.Id
        };
}

public sealed record TransactionCategoryExpense
{
    private static readonly CultureInfo Spanish = new("es-ES");

    public string CategoryName { get; init; } = string.Empty;
    public string TotalText { get; init; } = string.Empty;

    public static TransactionCategoryExpense From(CategoryExpenseSummary summary) =>
        new()
        {
            CategoryName = summary.CategoryName,
            TotalText = summary.Total.ToString("N2", Spanish)
        };
}

public sealed record TransactionMonthOption
{
    private static readonly CultureInfo Spanish = new("es-ES");

    public int Month { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsSelected { get; init; }

    public static TransactionMonthOption From(int month, int selectedMonth) =>
        new()
        {
            Month = month,
            Name = Spanish.TextInfo.ToTitleCase(Spanish.DateTimeFormat.MonthNames[month - 1]),
            IsSelected = month == selectedMonth
        };
}

public sealed record TransactionListOptions(
    string? Message,
    CreateTransactionInputModel? NewTransaction = null,
    SetBudgetInputModel? NewBudget = null,
    Guid? SelectedCategoryId = null);

public sealed record TransactionListViewModel
{
    private static readonly CultureInfo Spanish = new("es-ES");

    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthLabel { get; init; } = string.Empty;
    public string TotalIncomeText { get; init; } = string.Empty;
    public string TotalExpenseText { get; init; } = string.Empty;
    public string BalanceText { get; init; } = string.Empty;
    public IReadOnlyList<TransactionCategoryExpense> ExpenseByCategory { get; init; } = [];
    public IReadOnlyList<TransactionRowViewModel> Transactions { get; init; } = [];
    public IReadOnlyList<TransactionCategoryOption> CategoryOptions { get; init; } = [];
    public IReadOnlyList<TransactionCategoryOption> CreateCategoryOptions { get; init; } = [];
    public IReadOnlyList<TransactionMonthOption> MonthOptions { get; init; } = [];
    public Guid? SelectedCategoryId { get; init; }
    public CreateTransactionInputModel NewTransaction { get; init; } = new();
    public SetBudgetInputModel NewBudget { get; init; } = new();
    public IReadOnlyList<BudgetStatusViewModel> Budgets { get; init; } = [];
    public string? SuccessMessage { get; init; }
    public bool IsIncomeSelected { get; init; }
    public bool IsExpenseSelected { get; init; } = true;

    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);
    public bool HasTransactions => Transactions.Count > 0;
    public bool IsEmpty => Transactions.Count == 0;
    public bool HasCategories => CategoryOptions.Count > 0;
    public bool HasExpenseByCategory => ExpenseByCategory.Count > 0;
    public bool HasBudgets => Budgets.Count > 0;

    public static TransactionListViewModel From(
        MonthlyTransactionsResult result,
        IReadOnlyList<CategorySummary> categories,
        IReadOnlyList<BudgetStatusDto> budgets,
        TransactionListOptions options)
    {
        var current = options.NewTransaction ?? new();
        var monthOptions = new List<TransactionMonthOption>();
        for (var month = SetBudgetInputModel.MinMonth; month <= SetBudgetInputModel.MaxMonth; month++)
        {
            monthOptions.Add(TransactionMonthOption.From(month, result.Month));
        }
        return new()
        {
            Year = result.Year,
            Month = result.Month,
            MonthLabel = new DateTime(result.Year, result.Month, 1).ToString("MMMM yyyy", Spanish),
            TotalIncomeText = result.TotalIncome.ToString("N2", Spanish),
            TotalExpenseText = result.TotalExpense.ToString("N2", Spanish),
            BalanceText = result.Balance.ToString("N2", Spanish),
            ExpenseByCategory = result.ExpenseByCategory.Select(TransactionCategoryExpense.From).ToList(),
            Transactions = result.Transactions.Select(t => TransactionRowViewModel.From(t, categories)).ToList(),
            CategoryOptions = categories.Select(c => TransactionCategoryOption.From(c, options.SelectedCategoryId)).ToList(),
            CreateCategoryOptions = categories.Select(c => TransactionCategoryOption.From(c, current.CategoryId == Guid.Empty ? null : current.CategoryId)).ToList(),
            MonthOptions = monthOptions,
            SelectedCategoryId = options.SelectedCategoryId,
            NewTransaction = current,
            NewBudget = options.NewBudget ?? new() { Year = result.Year, Month = result.Month },
            Budgets = budgets.Select(b => BudgetStatusViewModel.From(b, result.Year, result.Month)).ToList(),
            SuccessMessage = options.Message,
            IsIncomeSelected = current.Type is true,
            IsExpenseSelected = current.Type is not true
        };
    }
}
