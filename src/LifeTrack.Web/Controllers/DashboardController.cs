using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.GetBudgetStatus;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Application.Finance.Queries.BalanceProjection;
using LifeTrack.Application.Finance.Queries.UnusualSpending;
using LifeTrack.Web.Services.Dashboard;
using LifeTrack.Web.ViewModels.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.Web.Controllers;

[Authorize]
[Route("dashboard")]
public sealed class DashboardController(
    ISender sender,
    ILogger<DashboardController> logger,
    TimeProvider timeProvider,
    IDashboardService dashboardService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? year, int? month, CancellationToken ct)
    {
        try
        {
            var monthly = await sender.Send(new ListMonthlyTransactionsQuery(year, month, null), ct);
            var budgets = await sender.Send(new GetBudgetStatusQuery(monthly.Year, monthly.Month, null), ct);
            var projection = await sender.Send(new GetBalanceProjectionQuery(monthly.Year, monthly.Month), ct);
            var alerts = await sender.Send(new GetUnusualSpendingAlertsQuery(monthly.Year, monthly.Month), ct);
            var comparison = await sender.Send(new GetMonthlyComparisonQuery(year, month), ct);

            var viewModel = DashboardViewModel.From(monthly, budgets, projection, alerts, comparison, timeProvider.Today(), dashboardService);
            return View(viewModel);
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning(ex, "Dashboard request failed");
            return RedirectToAction("Index", "Transactions");
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Dashboard resource not found");
            return RedirectToAction("Index", "Transactions");
        }
    }
}
