using System.Globalization;
using LifeTrack.Application.Finance.Commands.GetBudgetStatus;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Application.Finance.Queries.BalanceProjection;
using LifeTrack.Application.Finance.Queries.UnusualSpending;
using LifeTrack.Web.ViewModels.Dashboard;

namespace LifeTrack.Web.Services.Dashboard;

public sealed record DashboardViewData
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthLabel { get; init; } = string.Empty;
    public IReadOnlyList<MonthOptionViewModel> MonthOptions { get; init; } = [];
    public IReadOnlyList<YearOptionViewModel> YearOptions { get; init; } = [];
    public DashboardPeriodInputModel Period { get; init; } = new();
    public string TotalIncomeText { get; init; } = string.Empty;
    public string TotalExpenseText { get; init; } = string.Empty;
    public string BalanceText { get; init; } = string.Empty;
    public string ProjectedBalanceText { get; init; } = string.Empty;
    public string DailyAverageText { get; init; } = string.Empty;
    public string DaysRemainingText { get; init; } = string.Empty;
    public string ProjectionStatusText { get; init; } = string.Empty;
    public string ProjectionStatusClass { get; init; } = string.Empty;
    public bool HasIncomeTrend { get; init; }
    public string IncomeTrendText { get; init; } = string.Empty;
    public string IncomeTrendClass { get; init; } = string.Empty;
    public string IncomeContextText { get; init; } = string.Empty;
    public IReadOnlyList<SparklineBarViewModel> IncomeSparkline { get; init; } = [];
    public bool HasExpenseTrend { get; init; }
    public string ExpenseTrendText { get; init; } = string.Empty;
    public string ExpenseTrendClass { get; init; } = string.Empty;
    public string ExpenseContextText { get; init; } = string.Empty;
    public IReadOnlyList<SparklineBarViewModel> ExpenseSparkline { get; init; } = [];
    public int IncomeProgressPercent { get; init; }
    public int ExpenseProgressPercent { get; init; }
    public int BalanceProgressPercent { get; init; }
    public int ProjectionProgressPercent { get; init; }
    public SparklineBarViewModel ProjectionSparkline { get; init; } = new();
    public IReadOnlyList<AlertItemViewModel> Alerts { get; init; } = [];
    public IReadOnlyList<DonutSegmentViewModel> DonutSegments { get; init; } = [];
    public string DonutTotalText { get; init; } = string.Empty;
    public string DonutCenterLabel { get; init; } = string.Empty;
}
