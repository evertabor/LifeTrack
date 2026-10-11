using System.Globalization;
using LifeTrack.Application.Finance.Commands.GetBudgetStatus;

namespace LifeTrack.Web.ViewModels.Transactions;

public sealed record BudgetStatusViewModel
{
    private static readonly CultureInfo Spanish = new("es-ES");

    public string CategoryName { get; init; } = string.Empty;
    public string UsageText { get; init; } = string.Empty;
    public string PercentText { get; init; } = string.Empty;
    public string StateText { get; init; } = string.Empty;
    public string StatusCssClass { get; init; } = string.Empty;
    public string ProgressCssClass { get; init; } = string.Empty;
    public int FillPercent { get; init; }
    public int ProgressBarValue { get; init; }
    public string AriaLabel { get; init; } = string.Empty;

    public static BudgetStatusViewModel From(BudgetStatusDto dto)
    {
        var fill = dto.Percent < 0 ? 0 : dto.Percent > 100 ? 100 : (int)dto.Percent;
        var stateText = dto.State switch
        {
            "warning" => "Cerca del límite",
            "exceeded" => "Límite superado",
            _ => "Dentro del límite"
        };
        var statusCssClass = dto.State switch
        {
            "warning" => "app-badge app-badge--warning",
            "exceeded" => "app-badge app-badge--danger",
            _ => "app-badge app-badge--success"
        };
        var progressCssClass = dto.State switch
        {
            "warning" => "app-progress app-progress--warning",
            "exceeded" => "app-progress app-progress--exceeded",
            _ => "app-progress app-progress--ok"
        };
        var usageText = $"{dto.Spent.ToString("N2", Spanish)} de {dto.Limit.ToString("N2", Spanish)}";

        return new()
        {
            CategoryName = dto.CategoryName,
            UsageText = usageText,
            PercentText = $"{dto.Percent.ToString("N0", Spanish)} % usado",
            StateText = stateText,
            StatusCssClass = statusCssClass,
            ProgressCssClass = progressCssClass,
            FillPercent = fill,
            ProgressBarValue = fill,
            AriaLabel = $"Presupuesto {dto.CategoryName}: {usageText}"
        };
    }
}
