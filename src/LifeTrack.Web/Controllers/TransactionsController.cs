using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.CreateTransaction;
using LifeTrack.Application.Finance.Commands.ListCategories;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Web.ViewModels.Transactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.Web.Controllers;

[Route("transactions")]
[Authorize]
public sealed class TransactionsController(
    ISender sender,
    ILogger<TransactionsController> logger,
    TimeProvider timeProvider) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? year, int? month, Guid? categoryId, CancellationToken ct)
    {
        var today = timeProvider.Today();
        var selectedYear = year ?? today.Year;
        var selectedMonth = month ?? today.Month;

        try
        {
            var result = await sender.Send(new ListMonthlyTransactionsQuery(selectedYear, selectedMonth, categoryId), ct);
            var categories = await sender.Send(new ListCategoriesQuery(), ct);
            return View(TransactionListViewModel.From(result, categories, TempData["Message"] as string, null, categoryId));
        }
        catch (BusinessRuleException ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Transaction list requested with unknown category {CategoryId}: {Message}", categoryId, ex.Message);
            return RedirectToAction(nameof(Index), new { year = selectedYear, month = selectedMonth });
        }
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm(Name = "NewTransaction")] CreateTransactionInputModel input,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var categoryId = input.CategoryId;
            var amount = input.Amount;
            var date = input.Date;

            if (categoryId is null || amount is null || date is null)
            {
                ModelState.AddModelError(string.Empty, "Revisa los datos del formulario.");
                return await RenderIndexAsync(input, ct);
            }

            try
            {
                await sender.Send(new CreateTransactionCommand(
                    categoryId.Value, input.Type, amount.Value, date.Value, input.Description), ct);
                TempData["Message"] = "Movimiento registrado.";
                return RedirectToAction(nameof(Index), new { year = date.Value.Year, month = date.Value.Month });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (ResourceNotFoundException ex)
            {
                logger.LogWarning("Transaction create requested with unknown category {CategoryId}: {Message}", input.CategoryId, ex.Message);
                ModelState.AddModelError("NewTransaction.CategoryId", "La categoría no es válida.");
            }
        }

        return await RenderIndexAsync(input, ct);
    }

    private async Task<IActionResult> RenderIndexAsync(CreateTransactionInputModel input, CancellationToken ct)
    {
        var selected = input.Date ?? timeProvider.Today();

        MonthlyTransactionsResult result;
        try
        {
            result = await sender.Send(new ListMonthlyTransactionsQuery(selected.Year, selected.Month), ct);
        }
        catch (BusinessRuleException ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        var categories = await sender.Send(new ListCategoriesQuery(), ct);
        return View(nameof(Index), TransactionListViewModel.From(result, categories, null, input));
    }
}
