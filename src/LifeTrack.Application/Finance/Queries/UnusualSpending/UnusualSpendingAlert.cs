namespace LifeTrack.Application.Finance.Queries.UnusualSpending;

public sealed record UnusualSpendingAlert(
    Guid TransactionId,
    string CategoryName,
    decimal Amount,
    decimal CategoryAverage,
    decimal Ratio);
