using System.Globalization;
using LifeTrack.Application.Finance.Commands.GetBudgetStatus;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Application.Finance.Queries.BalanceProjection;
using LifeTrack.Application.Finance.Queries.UnusualSpending;
using LifeTrack.Web.Services.Dashboard;

namespace LifeTrack.Web.ViewModels.Dashboard;

public sealed record SparklineBarViewModel
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Height { get; init; }
    public string CssClass { get; init; } = string.Empty;
}

public sealed record AlertItemViewModel
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ActionController { get; init; }
    public string? ActionLabel { get; init; }
    public int? Month { get; init; }
    public int? Year { get; init; }

    public static AlertItemViewModel From(string type, string title, string message, string? actionController = null, string? actionLabel = null, int? month = null, int? year = null) =>
        new()
        {
            Type = type,
            Title = title,
            Message = message,
            ActionController = actionController,
            ActionLabel = actionLabel,
            Month = month,
            Year = year
        };
}

public sealed record DonutSegmentViewModel
{
    public string CategoryName { get; init; } = string.Empty;
    public string AmountText { get; init; } = string.Empty;
    public string PercentText { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public double StartAngle { get; init; }
    public double EndAngle { get; init; }
    public double Percent { get; init; }
    public bool IsSmallSegment { get; init; }
    public int FillPercent => Math.Clamp((int)Math.Round(Percent * 100, MidpointRounding.AwayFromZero), 0, 100);
    public string ProgressAriaLabel => $"Gasto en {CategoryName}: {PercentText}";
    public string StrokeDashArrayText => FormattableString.Invariant($"{VisualPercent:F4} {100.0 - VisualPercent:F4}");
    public string RotationText => (StartAngle - 90.0).ToString("F4", CultureInfo.InvariantCulture);

    private double VisualPercent => (EndAngle - StartAngle) / 3.6;
}

public sealed record MonthOptionViewModel
{
    private static readonly CultureInfo Spanish = new("es-ES");

    public int Month { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsSelected { get; init; }

    public static MonthOptionViewModel From(int month, int selectedMonth) =>
        new()
        {
            Month = month,
            Name = Spanish.TextInfo.ToTitleCase(Spanish.DateTimeFormat.MonthNames[month - 1]),
            IsSelected = month == selectedMonth
        };
}

public sealed record YearOptionViewModel
{
    public int Year { get; init; }
    public bool IsSelected { get; init; }

    public static YearOptionViewModel From(int year, int selectedYear) =>
        new()
        {
            Year = year,
            IsSelected = year == selectedYear
        };
}

public sealed record DashboardPeriodInputModel
{
    public int Year { get; init; }
    public int Month { get; init; }
}

public sealed record DashboardViewModel
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
    public bool HasAlerts => Alerts.Count > 0;
    public bool HasExpenses => DonutSegments.Count > 0;

    public static DashboardViewModel From(
        MonthlyTransactionsResult monthly,
        IReadOnlyList<BudgetStatusDto> budgets,
        BalanceProjectionResult projection,
        IReadOnlyList<UnusualSpendingAlert> alerts,
        MonthlyComparisonResult comparison,
        DateOnly today,
        IDashboardService dashboardService)
    {
        var data = dashboardService.Build(monthly, budgets, projection, alerts, comparison, today);

        return new DashboardViewModel
        {
            Year = data.Year,
            Month = data.Month,
            MonthLabel = data.MonthLabel,
            MonthOptions = data.MonthOptions,
            YearOptions = data.YearOptions,
            Period = data.Period,
            TotalIncomeText = data.TotalIncomeText,
            TotalExpenseText = data.TotalExpenseText,
            BalanceText = data.BalanceText,
            ProjectedBalanceText = data.ProjectedBalanceText,
            DailyAverageText = data.DailyAverageText,
            DaysRemainingText = data.DaysRemainingText,
            ProjectionStatusText = data.ProjectionStatusText,
            ProjectionStatusClass = data.ProjectionStatusClass,
            HasIncomeTrend = data.HasIncomeTrend,
            IncomeTrendText = data.IncomeTrendText,
            IncomeTrendClass = data.IncomeTrendClass,
            IncomeContextText = data.IncomeContextText,
            IncomeSparkline = data.IncomeSparkline,
            HasExpenseTrend = data.HasExpenseTrend,
            ExpenseTrendText = data.ExpenseTrendText,
            ExpenseTrendClass = data.ExpenseTrendClass,
            ExpenseContextText = data.ExpenseContextText,
            ExpenseSparkline = data.ExpenseSparkline,
            IncomeProgressPercent = data.IncomeProgressPercent,
            ExpenseProgressPercent = data.ExpenseProgressPercent,
            BalanceProgressPercent = data.BalanceProgressPercent,
            ProjectionProgressPercent = data.ProjectionProgressPercent,
            ProjectionSparkline = data.ProjectionSparkline,
            Alerts = data.Alerts,
            DonutSegments = data.DonutSegments,
            DonutTotalText = data.DonutTotalText,
            DonutCenterLabel = data.DonutCenterLabel
        };
    }
}
