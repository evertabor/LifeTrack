using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.CreateTransaction;
using LifeTrack.Application.Finance.Commands.DeleteTransaction;
using LifeTrack.Application.Finance.Commands.GetBudgetStatus;
using LifeTrack.Application.Finance.Commands.GetTransaction;
using LifeTrack.Application.Finance.Commands.ListCategories;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Application.Finance.Commands.UpdateTransaction;
using LifeTrack.Web.ViewModels.Transactions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.Web.Controllers;

[Route("transactions")]
public sealed class TransactionsController(
    ISender sender,
    ILogger<TransactionsController> logger,
    TimeProvider timeProvider) : TransactionsSectionController(sender, logger)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? year, int? month, Guid? categoryId, CancellationToken ct)
    {
        try
        {
            var result = await Sender.Send(new ListMonthlyTransactionsQuery(year, month, categoryId), ct);
            var categories = await Sender.Send(new ListCategoriesQuery(), ct);
            var budgets = await Sender.Send(new GetBudgetStatusQuery(result.Year, result.Month, categoryId), ct);
            return View(TransactionListViewModel.From(result, categories, budgets, new TransactionListOptions(TempData["Message"] as string, null, null, categoryId, timeProvider.Today())));
        }
        catch (BusinessRuleException ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Transaction list requested with unknown category {CategoryId}", categoryId);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(
        [FromForm(Name = "NewTransaction")] CreateTransactionInputModel input,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var categoryId = input.CategoryId;
            var type = input.Type;
            var amount = input.Amount;
            var date = input.Date;

            if (categoryId is null || type is null || amount is null || date is null)
            {
                ModelState.AddModelError("NewTransaction", "Revisa los datos del formulario.");
                return await RenderTransactionsIndexAsync(new IndexRenderOptions(date ?? timeProvider.Today(), input, null, null, timeProvider.Today()), ct);
            }

            try
            {
                await Sender.Send(new CreateTransactionCommand(
                    categoryId.Value, type.Value, amount.Value, date.Value, input.Description), ct);
                TempData["Message"] = "Movimiento registrado.";
                return RedirectToAction(nameof(Index), new { year = date.Value.Year, month = date.Value.Month });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError("NewTransaction", ex.Message);
            }
            catch (ResourceNotFoundException ex)
            {
                logger.LogWarning(ex, "Transaction create requested with unknown category {CategoryId}", input.CategoryId);
                ModelState.AddModelError("NewTransaction.CategoryId", "La categoría no es válida.");
            }
        }

        return await RenderTransactionsIndexAsync(new IndexRenderOptions(input.Date ?? timeProvider.Today(), input, null, null, timeProvider.Today()), ct);
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        try
        {
            var transaction = await Sender.Send(new GetTransactionQuery(id), ct);
            var categories = await Sender.Send(new ListCategoriesQuery(), ct);
            return View(TransactionEditViewModel.From(transaction, categories, timeProvider.Today()));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Transaction edit requested with unknown movement {TransactionId}", id);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("{id:guid}/edit")]
    public async Task<IActionResult> Edit(
        Guid id,
        [FromForm(Name = "Input")] EditTransactionInputModel input,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var categoryId = input.CategoryId;
            var type = input.Type;
            var amount = input.Amount;
            var date = input.Date;

            if (categoryId is null || type is null || amount is null || date is null)
            {
                ModelState.AddModelError(string.Empty, "Revisa los datos del formulario.");
            }
            else
            {
                try
                {
                    await Sender.Send(new UpdateTransactionCommand(
                        id, categoryId.Value, type.Value, amount.Value, date.Value, input.Description), ct);
                    TempData["Message"] = "Movimiento actualizado.";
                    return RedirectToAction(nameof(Index), new { year = date.Value.Year, month = date.Value.Month });
                }
                catch (BusinessRuleException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (ResourceNotFoundException ex)
                {
                    logger.LogWarning(ex, "Transaction edit requested with unknown movement {TransactionId}", id);
                    return RedirectToAction(nameof(Index));
                }
            }
        }

        var categories = await Sender.Send(new ListCategoriesQuery(), ct);
        return View(nameof(Edit), TransactionEditViewModel.From(id, input, categories, timeProvider.Today()));
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await Sender.Send(new DeleteTransactionCommand(id), ct);
            TempData["Message"] = "Movimiento eliminado.";
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Transaction delete requested with unknown movement {TransactionId}", id);
        }

        return RedirectToAction(nameof(Index));
    }
}
