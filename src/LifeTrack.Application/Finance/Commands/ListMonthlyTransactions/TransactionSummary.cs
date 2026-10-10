using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;

public sealed record TransactionSummary(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    bool IsIncome,
    string TypeLabel,
    decimal Amount,
    DateOnly Date,
    string Description);
