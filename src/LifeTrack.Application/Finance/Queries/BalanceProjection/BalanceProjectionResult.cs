namespace LifeTrack.Application.Finance.Queries.BalanceProjection;

public sealed record BalanceProjectionResult(
    decimal ProjectedBalance,
    int DaysElapsed,
    int DaysRemaining,
    decimal DailyAverageExpense,
    bool IsNegative);

public sealed record MonthlyComparisonResult(
    decimal PreviousMonthIncome,
    decimal PreviousMonthExpense,
    decimal IncomeChangePercent,
    decimal ExpenseChangePercent);
