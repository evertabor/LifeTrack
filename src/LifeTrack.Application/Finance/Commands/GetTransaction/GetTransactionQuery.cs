using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;

namespace LifeTrack.Application.Finance.Commands.GetTransaction;

public sealed record GetTransactionQuery(Guid Id) : IQuery<TransactionSummary>;
