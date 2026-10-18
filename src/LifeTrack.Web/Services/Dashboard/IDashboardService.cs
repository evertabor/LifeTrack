using LifeTrack.Application.Finance.Commands.GetBudgetStatus;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Application.Finance.Queries.BalanceProjection;
using LifeTrack.Application.Finance.Queries.UnusualSpending;
using LifeTrack.Web.ViewModels.Dashboard;

namespace LifeTrack.Web.Services.Dashboard;

public interface IDashboardService
{
    DashboardViewData Build(
        MonthlyTransactionsResult monthly,
        IReadOnlyList<BudgetStatusDto> budgets,
        BalanceProjectionResult projection,
        IReadOnlyList<UnusualSpendingAlert> alerts,
        MonthlyComparisonResult comparison,
        DateOnly today);
}
