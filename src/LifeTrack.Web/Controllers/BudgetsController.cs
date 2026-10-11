using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.SetBudget;
using LifeTrack.Web.ViewModels.Transactions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.Web.Controllers;

[Route("transactions/budgets")]
public sealed class BudgetsController(
    ISender sender,
    ILogger<BudgetsController> logger,
    TimeProvider timeProvider) : TransactionsSectionController(sender, logger)
{
    [HttpPost("")]
    public async Task<IActionResult> SetBudget(
        [FromForm(Name = "NewBudget")] SetBudgetInputModel input,
        CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var categoryId = input.CategoryId;
            var limit = input.Limit;

            if (categoryId is null || limit is null)
            {
                ModelState.AddModelError(string.Empty, "Revisa los datos del formulario.");
            }
            else
            {
                try
                {
                    await Sender.Send(new SetBudgetCommand(categoryId.Value, input.Year, input.Month, limit.Value), ct);
                    TempData["Message"] = "Presupuesto guardado.";
                    return RedirectToAction(nameof(TransactionsController.Index), "Transactions", new { year = input.Year, month = input.Month });
                }
                catch (BusinessRuleException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (ResourceNotFoundException ex)
                {
                    logger.LogWarning(ex, "Budget set requested with unknown category {CategoryId}", input.CategoryId);
                    ModelState.AddModelError("NewBudget.CategoryId", "La categoría no es válida.");
                }
            }
        }

        var safeYear = input.Year is >= SetBudgetInputModel.MinYear and <= SetBudgetInputModel.MaxYear ? input.Year : timeProvider.Today().Year;
        var safeMonth = input.Month is >= SetBudgetInputModel.MinMonth and <= SetBudgetInputModel.MaxMonth ? input.Month : timeProvider.Today().Month;
        return await RenderTransactionsIndexAsync(new IndexRenderOptions(new DateOnly(safeYear, safeMonth, 1), null, input, null), ct);
    }
}
