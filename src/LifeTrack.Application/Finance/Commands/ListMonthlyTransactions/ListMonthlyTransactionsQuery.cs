using LifeTrack.Application.Common;

namespace LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;

public sealed record ListMonthlyTransactionsQuery(int Year, int Month, Guid? CategoryId = null)
    : IQuery<MonthlyTransactionsResult>;
