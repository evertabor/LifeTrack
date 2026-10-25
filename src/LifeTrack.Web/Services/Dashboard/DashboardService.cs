using System.Globalization;
using LifeTrack.Application.Finance.Commands.GetBudgetStatus;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Application.Finance.Queries.BalanceProjection;
using LifeTrack.Application.Finance.Queries.UnusualSpending;
using LifeTrack.Web.ViewModels.Dashboard;

namespace LifeTrack.Web.Services.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private static readonly CultureInfo Spanish = new("es-ES");

    private const int MIN_VISUAL_ANGLE = 12;
    private const int SPARKLINE_MAX_HEIGHT = 20;
    private const int SPARKLINE_MIN_HEIGHT = 4;
    private const int SPARKLINE_PREV_X = 16;
    private const int SPARKLINE_CURR_X = 52;
    private const int SPARKLINE_BASE_Y = 28;
    private const double FULL_CIRCLE_DEGREES = 360.0;
    private const int PERCENT_MULTIPLIER = 100;
    private const int PROGRESS_MAX = 100;
    private const int PROGRESS_MIN = 0;
    private const int MIN_SEGMENT_ANGLE = 45;
    private const int YEARS_RANGE = 5;
    private const int MONTHS_IN_YEAR = 12;

    private static readonly string[] DONUT_COLORS =
    {
        "#2878B5",
        "#EA580C",
        "#15803D",
        "#9A3412",
        "#0369A1",
        "#C2410C"
    };

    private static readonly string PROJECTION_NEGATIVE_STATUS = "app-badge app-badge--danger";
    private static readonly string PROJECTION_CONTROLLED_STATUS = "app-badge app-badge--success";
    private static readonly string PROJECTION_NEUTRAL_STATUS = "app-badge app-badge--neutral";

    private static readonly string PROJECTION_NEGATIVE_TEXT = "Riesgo de d\u00E9ficit";
    private static readonly string PROJECTION_CONTROLLED_TEXT = "Ritmo controlado";
    private static readonly string PROJECTION_NEUTRAL_TEXT = "Sin gasto registrado";

    private static readonly string TREND_UP_CLASS = "app-stat-card__trend--up";
    private static readonly string TREND_DOWN_CLASS = "app-stat-card__trend--down";

    private static readonly string SPARKLINE_SUCCESS_CLASS = "app-sparkline__bar--success";
    private static readonly string SPARKLINE_DANGER_CLASS = "app-sparkline__bar--danger";
    private static readonly string SPARKLINE_NEUTRAL_CLASS = "app-sparkline__bar--neutral";

    private static readonly string ALERT_DANGER_TYPE = "danger";
    private static readonly string ALERT_WARNING_TYPE = "warning";
    private static readonly string ALERT_INFO_TYPE = "info";

    private static readonly string BUDGET_EXCEEDED_LABEL = "L\u00EDmite superado";
    private static readonly string BUDGET_WARNING_LABEL = "Cerca del l\u00EDmite";
    private static readonly string UNUSUAL_SPENDING_TITLE = "Gasto inusual";
    private static readonly string NEGATIVE_PROJECTION_TITLE = "Proyecci\u00F3n negativa";
    private static readonly string PROJECTION_INFO_TITLE = "Proyecci\u00F3n de saldo";
    private static readonly string NO_PREVIOUS_MONTH_DATA = "Sin datos del mes anterior";

    public DashboardViewData Build(
        MonthlyTransactionsResult monthly,
        IReadOnlyList<BudgetStatusDto> budgets,
        BalanceProjectionResult projection,
        IReadOnlyList<UnusualSpendingAlert> alerts,
        MonthlyComparisonResult comparison,
        DateOnly today)
    {
        var alertsList = BuildAlerts(monthly, budgets, projection, alerts);
        var donutSegments = BuildDonutSegments(monthly);
        var totalExpense = monthly.ExpenseByCategory.Sum(e => e.Total);

        var (hasIncomeTrend, incomeTrendText, incomeTrendClass, incomeContextText) = BuildTrendInfo(comparison.IncomeChangePercent, comparison.PreviousMonthIncome);
        var (hasExpenseTrend, expenseTrendText, expenseTrendClass, expenseContextText) = BuildTrendInfo(comparison.ExpenseChangePercent, comparison.PreviousMonthExpense, isExpense: true);

        var (incomeProgress, expenseProgress, balanceProgress, projectionProgress) = CalculateProgress(monthly, budgets, projection);
        var incomeSparkline = BuildSparkline(comparison.PreviousMonthIncome, monthly.TotalIncome, SPARKLINE_SUCCESS_CLASS);
        var expenseSparkline = BuildSparkline(comparison.PreviousMonthExpense, monthly.TotalExpense, SPARKLINE_DANGER_CLASS);
        var hasIncomeSparkline = comparison.PreviousMonthIncome > 0m || monthly.TotalIncome > 0m;
        var hasExpenseSparkline = comparison.PreviousMonthExpense > 0m || monthly.TotalExpense > 0m;

        var (projectionStatusText, projectionStatusClass) = BuildProjectionStatus(projection);

        var monthLabel = new DateTime(monthly.Year, monthly.Month, 1).ToString("MMMM yyyy", Spanish);
        var maxMonth = monthly.Year == today.Year ? today.Month : MONTHS_IN_YEAR;
        var monthOptions = Enumerable.Range(1, maxMonth)
            .Select(m => MonthOptionViewModel.From(m, monthly.Month))
            .ToList();
        var yearOptions = Enumerable.Range(today.Year - YEARS_RANGE, YEARS_RANGE + 1)
            .Select(y => YearOptionViewModel.From(y, monthly.Year))
            .ToList();

        return new DashboardViewData
        {
            Year = monthly.Year,
            Month = monthly.Month,
            MonthLabel = monthLabel,
            MonthOptions = monthOptions,
            YearOptions = yearOptions,
            Period = new DashboardPeriodInputModel { Year = monthly.Year, Month = monthly.Month },
            TotalIncomeText = monthly.TotalIncome.ToString("N2", Spanish),
            TotalExpenseText = monthly.TotalExpense.ToString("N2", Spanish),
            BalanceText = monthly.Balance.ToString("N2", Spanish),
            ProjectedBalanceText = projection.ProjectedBalance.ToString("N2", Spanish),
            DailyAverageText = projection.DailyAverageExpense.ToString("N2", Spanish),
            DaysRemainingText = projection.DaysRemaining.ToString(),
            ProjectionStatusText = projectionStatusText,
            ProjectionStatusClass = projectionStatusClass,
            HasIncomeTrend = hasIncomeTrend,
            IncomeTrendText = incomeTrendText,
            IncomeTrendClass = incomeTrendClass,
            IncomeContextText = incomeContextText,
            IncomeSparkline = incomeSparkline,
            HasExpenseTrend = hasExpenseTrend,
            ExpenseTrendText = expenseTrendText,
            ExpenseTrendClass = expenseTrendClass,
            ExpenseContextText = expenseContextText,
            ExpenseSparkline = expenseSparkline,
            IncomeProgressPercent = incomeProgress,
            ExpenseProgressPercent = expenseProgress,
            BalanceProgressPercent = balanceProgress,
            ProjectionProgressPercent = projectionProgress,
            ProjectionSparkline = new SparklineBarViewModel(),
            Alerts = alertsList,
            DonutSegments = donutSegments,
            DonutTotalText = totalExpense.ToString("N2", Spanish),
            DonutCenterLabel = "Total gastado"
        };
    }

    private static List<AlertItemViewModel> BuildAlerts(
        MonthlyTransactionsResult monthly,
        IReadOnlyList<BudgetStatusDto> budgets,
        BalanceProjectionResult projection,
        IReadOnlyList<UnusualSpendingAlert> alerts)
    {
        var alertsList = new List<AlertItemViewModel>();
        var projectedBalanceText = projection.ProjectedBalance.ToString("N2", Spanish);
        var hasActivity = monthly.TotalIncome > 0m || monthly.TotalExpense > 0m;

        if (projection.IsNegative && hasActivity)
        {
            alertsList.Add(AlertItemViewModel.From(
                ALERT_DANGER_TYPE,
                NEGATIVE_PROJECTION_TITLE,
                $"A este ritmo gastar\u00E1s m\u00E1s de lo que ingresas. Proyecci\u00F3n a fin de mes: {projectedBalanceText}.",
                "Transactions",
                "Revisar movimientos",
                monthly.Month,
                monthly.Year));
        }
        else if (projection.DaysRemaining > 0 && projection.DailyAverageExpense > 0m)
        {
            alertsList.Add(AlertItemViewModel.From(
                ALERT_INFO_TYPE,
                PROJECTION_INFO_TITLE,
                $"A este ritmo te quedar\u00E1n {projectedBalanceText} a fin de mes.",
                "Transactions",
                "Revisar movimientos",
                monthly.Month,
                monthly.Year));
        }

        foreach (var budget in budgets.Where(b => b.State is "warning" or "exceeded"))
        {
            var stateLabel = budget.State == "exceeded" ? BUDGET_EXCEEDED_LABEL : BUDGET_WARNING_LABEL;
            alertsList.Add(AlertItemViewModel.From(
                budget.State == "exceeded" ? ALERT_DANGER_TYPE : ALERT_WARNING_TYPE,
                $"Presupuesto: {budget.CategoryName}",
                $"{stateLabel}. {budget.Percent:N0}% usado ({budget.Spent.ToString("N2", Spanish)} de {budget.Limit.ToString("N2", Spanish)}).",
                "Transactions",
                "Ver presupuesto",
                monthly.Month,
                monthly.Year));
        }

        foreach (var unusual in alerts)
        {
            alertsList.Add(AlertItemViewModel.From(
                ALERT_WARNING_TYPE,
                UNUSUAL_SPENDING_TITLE,
                $"{unusual.CategoryName}: has gastado {unusual.Amount.ToString("N2", Spanish)} cuando tu media es {unusual.CategoryAverage.ToString("N2", Spanish)} ({unusual.Ratio:N1}x).",
                "Transactions",
                "Revisar categor\u00EDa",
                monthly.Month,
                monthly.Year));
        }

        return alertsList;
    }

    private static List<DonutSegmentViewModel> BuildDonutSegments(MonthlyTransactionsResult monthly)
    {
        var totalExpense = monthly.ExpenseByCategory.Sum(e => e.Total);
        var segments = new List<DonutSegmentViewModel>();

        var expenseCategories = monthly.ExpenseByCategory
            .Where(e => e.Total > 0)
            .OrderByDescending(e => e.Total)
            .ToList();

        var currentAngle = 0.0;

        for (var i = 0; i < expenseCategories.Count; i++)
        {
            var cat = expenseCategories[i];
            var percent = totalExpense > 0m ? (double)(cat.Total / totalExpense) : 0.0;
            var proportionalAngle = percent * FULL_CIRCLE_DEGREES;
            var sweepAngle = Math.Max(proportionalAngle, MIN_VISUAL_ANGLE);

            segments.Add(new DonutSegmentViewModel
            {
                CategoryName = cat.CategoryName,
                AmountText = cat.Total.ToString("N2", Spanish),
                PercentText = (percent * PERCENT_MULTIPLIER).ToString("N1", Spanish) + "%",
                Color = DONUT_COLORS[i % DONUT_COLORS.Length],
                StartAngle = currentAngle,
                EndAngle = currentAngle + sweepAngle,
                Percent = percent,
                IsSmallSegment = sweepAngle < MIN_SEGMENT_ANGLE
            });

            currentAngle += sweepAngle;
        }

        var totalVisualAngle = segments.Sum(s => s.EndAngle - s.StartAngle);
        if (totalVisualAngle > FULL_CIRCLE_DEGREES)
        {
            var scale = FULL_CIRCLE_DEGREES / totalVisualAngle;
            var scaledSegments = new List<DonutSegmentViewModel>();
            currentAngle = 0.0;

            foreach (var segment in segments)
            {
                var sweepAngle = (segment.EndAngle - segment.StartAngle) * scale;
                scaledSegments.Add(segment with
                {
                    StartAngle = currentAngle,
                    EndAngle = currentAngle + sweepAngle,
                    IsSmallSegment = sweepAngle < MIN_SEGMENT_ANGLE
                });
                currentAngle += sweepAngle;
            }

            segments = scaledSegments;
        }

        return segments;
    }

    private static (bool HasTrend, string TrendText, string TrendClass, string ContextText) BuildTrendInfo(decimal changePercent, decimal previousValue, bool isExpense = false)
    {
        var hasTrend = changePercent != 0m;
        var trendText = string.Empty;
        var trendClass = string.Empty;
        var contextText = string.Empty;

        if (hasTrend)
        {
            var prefix = changePercent >= 0 ? "+" : "";
            trendText = $"{prefix}{changePercent.ToString("N0", Spanish)}%";
            trendClass = isExpense
                ? (changePercent <= 0 ? TREND_UP_CLASS : TREND_DOWN_CLASS)
                : (changePercent >= 0 ? TREND_UP_CLASS : TREND_DOWN_CLASS);
            contextText = $"{previousValue.ToString("N2", Spanish)} mes anterior";
        }
        else
        {
            contextText = NO_PREVIOUS_MONTH_DATA;
        }

        return (hasTrend, trendText, trendClass, contextText);
    }

    private static (int IncomeProgress, int ExpenseProgress, int BalanceProgress, int ProjectionProgress) CalculateProgress(
        MonthlyTransactionsResult monthly,
        IReadOnlyList<BudgetStatusDto> budgets,
        BalanceProjectionResult projection)
    {
        var incomeProgress = monthly.TotalIncome > 0m ? PROGRESS_MAX : PROGRESS_MIN;
        var expenseProgress = budgets.Count > 0
            ? Math.Min((int)budgets.Average(b => b.Percent), PROGRESS_MAX)
            : PROGRESS_MIN;
        var balanceProgress = monthly.TotalIncome > 0m
            ? Math.Max(PROGRESS_MIN, Math.Min((int)(monthly.Balance / monthly.TotalIncome * PERCENT_MULTIPLIER), PROGRESS_MAX))
            : PROGRESS_MIN;
        var projectionProgress = projection.ProjectedBalance >= 0m && monthly.TotalIncome > 0m
            ? Math.Min((int)(projection.ProjectedBalance / monthly.TotalIncome * PERCENT_MULTIPLIER), PROGRESS_MAX)
            : PROGRESS_MIN;

        return (incomeProgress, expenseProgress, balanceProgress, projectionProgress);
    }

    private static List<SparklineBarViewModel> BuildSparkline(decimal previous, decimal current, string cssClass)
    {
        var bars = new List<SparklineBarViewModel>();

        if (previous <= 0m && current <= 0m)
        {
            bars.Add(new SparklineBarViewModel { X = 0, Y = SPARKLINE_BASE_Y - SPARKLINE_MIN_HEIGHT, Height = SPARKLINE_MIN_HEIGHT, CssClass = SPARKLINE_NEUTRAL_CLASS });
            return bars;
        }

        var max = Math.Max(previous, current);
        var prevHeight = previous > 0m ? Math.Max(SPARKLINE_MIN_HEIGHT, (double)(previous / max * SPARKLINE_MAX_HEIGHT)) : SPARKLINE_MIN_HEIGHT;
        var currHeight = current > 0m ? Math.Max(SPARKLINE_MIN_HEIGHT, (double)(current / max * SPARKLINE_MAX_HEIGHT)) : SPARKLINE_MIN_HEIGHT;

        bars.Add(new SparklineBarViewModel
        {
            X = SPARKLINE_PREV_X,
            Y = SPARKLINE_BASE_Y - prevHeight,
            Height = prevHeight,
            CssClass = SPARKLINE_NEUTRAL_CLASS
        });

        bars.Add(new SparklineBarViewModel
        {
            X = SPARKLINE_CURR_X,
            Y = SPARKLINE_BASE_Y - currHeight,
            Height = currHeight,
            CssClass = cssClass
        });

        return bars;
    }

    private static (string StatusText, string StatusClass) BuildProjectionStatus(BalanceProjectionResult projection)
    {
        if (projection.IsNegative)
            return (PROJECTION_NEGATIVE_TEXT, PROJECTION_NEGATIVE_STATUS);

        if (projection.DailyAverageExpense > 0m)
            return (PROJECTION_CONTROLLED_TEXT, PROJECTION_CONTROLLED_STATUS);

        return (PROJECTION_NEUTRAL_TEXT, PROJECTION_NEUTRAL_STATUS);
    }
}
