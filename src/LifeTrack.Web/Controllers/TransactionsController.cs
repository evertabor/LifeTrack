using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.CreateTransaction;
using LifeTrack.Application.Finance.Commands.DeleteTransaction;
using LifeTrack.Application.Finance.Commands.GetBudgetStatus;
using LifeTrack.Application.Finance.Commands.GetTransaction;
using LifeTrack.Application.Finance.Commands.ImportTransactions;
using LifeTrack.Application.Finance.Commands.ListCategories;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Application.Finance.Commands.SetBudget;
using LifeTrack.Application.Finance.Commands.UpdateTransaction;
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
    private const long MaxImportBytes = 1024 * 1024;

    [HttpGet("")]
    public async Task<IActionResult> Index(int? year, int? month, Guid? categoryId, CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(new ListMonthlyTransactionsQuery(year, month, categoryId), ct);
            var categories = await sender.Send(new ListCategoriesQuery(), ct);
            var budgets = await sender.Send(new GetBudgetStatusQuery(result.Year, result.Month), ct);
            return View(TransactionListViewModel.From(result, categories, budgets, new TransactionListOptions(TempData["Message"] as string, null, null, categoryId)));
        }
        catch (BusinessRuleException ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Transaction list requested with unknown category {CategoryId}: {Message}", categoryId, ex.Message);
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
                ModelState.AddModelError(string.Empty, "Revisa los datos del formulario.");
                return await RenderIndexAsync(new IndexRenderOptions(date ?? timeProvider.Today(), input, null, null), ct);
            }

            try
            {
                await sender.Send(new CreateTransactionCommand(
                    categoryId.Value, type.Value, amount.Value, date.Value, input.Description), ct);
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

        return await RenderIndexAsync(new IndexRenderOptions(input.Date ?? timeProvider.Today(), input, null, null), ct);
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        try
        {
            var transaction = await sender.Send(new GetTransactionQuery(id), ct);
            var categories = await sender.Send(new ListCategoriesQuery(), ct);
            return View(TransactionEditViewModel.From(transaction, categories));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Transaction edit requested with unknown movement {TransactionId}: {Message}", id, ex.Message);
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
                    await sender.Send(new UpdateTransactionCommand(
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
                    logger.LogWarning("Transaction edit requested with unknown movement {TransactionId}: {Message}", id, ex.Message);
                    return RedirectToAction(nameof(Index));
                }
            }
        }

        var categories = await sender.Send(new ListCategoriesQuery(), ct);
        return View(nameof(Edit), TransactionEditViewModel.From(id, input, categories));
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeleteTransactionCommand(id), ct);
            TempData["Message"] = "Movimiento eliminado.";
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Transaction delete requested with unknown movement {TransactionId}: {Message}", id, ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("budgets")]
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
                    await sender.Send(new SetBudgetCommand(categoryId.Value, input.Year, input.Month, limit.Value), ct);
                    TempData["Message"] = "Presupuesto guardado.";
                    return RedirectToAction(nameof(Index), new { year = input.Year, month = input.Month });
                }
                catch (BusinessRuleException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (ResourceNotFoundException ex)
                {
                    logger.LogWarning("Budget set requested with unknown category {CategoryId}: {Message}", input.CategoryId, ex.Message);
                    ModelState.AddModelError("NewBudget.CategoryId", "La categoría no es válida.");
                }
            }
        }

        var safeYear = input.Year is >= SetBudgetInputModel.MinYear and <= SetBudgetInputModel.MaxYear ? input.Year : timeProvider.Today().Year;
        var safeMonth = input.Month is >= SetBudgetInputModel.MinMonth and <= SetBudgetInputModel.MaxMonth ? input.Month : timeProvider.Today().Month;
        return await RenderIndexAsync(new IndexRenderOptions(new DateOnly(safeYear, safeMonth, 1), null, input, null), ct);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Message"] = "Selecciona un archivo CSV para importar.";
            return RedirectToAction(nameof(Index));
        }

        if (file.Length > MaxImportBytes)
        {
            TempData["Message"] = "El archivo supera el máximo de 1 MB.";
            return RedirectToAction(nameof(Index));
        }

        if (!".csv".Equals(Path.GetExtension(file.FileName), StringComparison.OrdinalIgnoreCase))
        {
            TempData["Message"] = "El archivo debe tener extensión .csv.";
            return RedirectToAction(nameof(Index));
        }

        string content;
        using (var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            content = await reader.ReadToEndAsync(ct);
        }

        try
        {
            var preview = await sender.Send(new PreviewTransactionsQuery(content), ct);
            return View("ImportPreview", ImportPreviewViewModel.From(preview, content));
        }
        catch (BusinessRuleException ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("import/confirm")]
    public async Task<IActionResult> ConfirmImport([FromForm] string? csvContent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            TempData["Message"] = "La vista previa caducó. Vuelve a subir el archivo.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var imported = await sender.Send(new ConfirmTransactionImportCommand(csvContent), ct);
            TempData["Message"] = $"Se importaron {imported} movimientos.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleException ex)
        {
            TempData["Message"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    private sealed record IndexRenderOptions(
        DateOnly Selected,
        CreateTransactionInputModel? TransactionInput,
        SetBudgetInputModel? BudgetInput,
        Guid? CategoryId);

    private async Task<IActionResult> RenderIndexAsync(
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
            return RedirectToAction(nameof(Index));
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning("Transaction list requested with unknown category {CategoryId}: {Message}", options.CategoryId, ex.Message);
            return RedirectToAction(nameof(Index), new { year = options.Selected.Year, month = options.Selected.Month });
        }

        var categories = await sender.Send(new ListCategoriesQuery(), ct);
        var budgets = await sender.Send(new GetBudgetStatusQuery(options.Selected.Year, options.Selected.Month), ct);
        return View(nameof(Index), TransactionListViewModel.From(result, categories, budgets, new TransactionListOptions(null, options.TransactionInput, options.BudgetInput, options.CategoryId)));
    }
}
