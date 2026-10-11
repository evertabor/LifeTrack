using LifeTrack.Application.Common;

namespace LifeTrack.Application.Finance.Commands.GetBudgetStatus;

public sealed record GetBudgetStatusQuery(int Year, int Month)
    : IQuery<IReadOnlyList<BudgetStatusDto>>;
