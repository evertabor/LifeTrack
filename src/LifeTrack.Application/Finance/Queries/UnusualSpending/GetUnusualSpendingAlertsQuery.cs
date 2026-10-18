using LifeTrack.Application.Common;

namespace LifeTrack.Application.Finance.Queries.UnusualSpending;

public sealed record GetUnusualSpendingAlertsQuery(int? Year, int? Month) : IQuery<IReadOnlyList<UnusualSpendingAlert>>;
