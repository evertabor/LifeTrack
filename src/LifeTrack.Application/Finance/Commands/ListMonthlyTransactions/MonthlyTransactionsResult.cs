namespace LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;

public sealed record MonthlyTransactionsResult(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpense,
    IReadOnlyList<CategoryExpenseSummary> ExpenseByCategory,
    IReadOnlyList<TransactionSummary> Transactions)
{
    public decimal Balance => TotalIncome - TotalExpense;
}
