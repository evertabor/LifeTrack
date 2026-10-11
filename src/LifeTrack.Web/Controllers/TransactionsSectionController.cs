using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.GetBudgetStatus;
using LifeTrack.Application.Finance.Commands.ListCategories;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Web.ViewModels.Transactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.Web.Controllers;

[Authorize]
public abstract class TransactionsSectionController(
    ISender sender,
    ILogger logger) : Controller
{
    protected ISender Sender => sender;

    protected sealed record IndexRenderOptions(
        DateOnly Selected,
        CreateTransactionInputModel? TransactionInput,
        SetBudgetInputModel? BudgetInput,
        Guid? CategoryId);

    protected async Task<IActionResult> RenderTransactionsIndexAsync(
        IndexRenderOptions options,
        CancellationToken ct)
    {
        MonthlyTransactionsResult result;
        try
        {
            result = await sender.Send(new ListMonthlyTransactionsQuery(options.Selected.Year, options.Selected.Month, options.CategoryId), ct);
        }
        catch (BusinessRuleException ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToAction(nameof(TransactionsController.Index), "Transactions");
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Transaction list requested with unknown category {CategoryId}: {Message}", options.CategoryId, ex.Message);
            return RedirectToAction(nameof(TransactionsController.Index), "Transactions", new { year = options.Selected.Year, month = options.Selected.Month });
        }

        var categories = await sender.Send(new ListCategoriesQuery(), ct);
        var budgets = await sender.Send(new GetBudgetStatusQuery(options.Selected.Year, options.Selected.Month), ct);
        return View("~/Views/Transactions/Index.cshtml", TransactionListViewModel.From(result, categories, budgets, new TransactionListOptions(null, options.TransactionInput, options.BudgetInput, options.CategoryId)));
    }
}
