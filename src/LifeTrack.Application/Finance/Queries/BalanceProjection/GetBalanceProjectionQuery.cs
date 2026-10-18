using LifeTrack.Application.Common;

namespace LifeTrack.Application.Finance.Queries.BalanceProjection;

public sealed record GetBalanceProjectionQuery(int? Year, int? Month) : IQuery<BalanceProjectionResult>;

public sealed record GetMonthlyComparisonQuery(int? Year, int? Month) : IQuery<MonthlyComparisonResult>;
