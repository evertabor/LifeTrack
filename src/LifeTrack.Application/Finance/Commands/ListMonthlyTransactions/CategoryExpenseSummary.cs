namespace LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;

public sealed record CategoryExpenseSummary(
    Guid CategoryId,
    string CategoryName,
    decimal Total);
