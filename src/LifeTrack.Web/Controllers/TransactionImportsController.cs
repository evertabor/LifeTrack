using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.ImportTransactions;
using LifeTrack.Web.ViewModels.Transactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.Web.Controllers;

[Route("transactions/import")]
[Authorize]
public sealed class TransactionImportsController(
    ISender sender,
    ILogger<TransactionImportsController> logger) : Controller
{
    private const long MaxImportBytes = 1024 * 1024;

    [HttpPost("")]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Message"] = "Selecciona un archivo CSV para importar.";
            return RedirectToAction(nameof(TransactionsController.Index), "Transactions");
        }

        if (file.Length > MaxImportBytes)
        {
            TempData["Message"] = "El archivo supera el máximo de 1 MB.";
            return RedirectToAction(nameof(TransactionsController.Index), "Transactions");
        }

        if (!".csv".Equals(Path.GetExtension(file.FileName), StringComparison.OrdinalIgnoreCase))
        {
            TempData["Message"] = "El archivo debe tener extensión .csv.";
            return RedirectToAction(nameof(TransactionsController.Index), "Transactions");
        }

        string content;
        using (var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            content = await reader.ReadToEndAsync(ct);
        }

        try
        {
            var preview = await sender.Send(new PreviewTransactionsQuery(content), ct);
            return View("~/Views/Transactions/ImportPreview.cshtml", ImportPreviewViewModel.From(preview, content));
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning(ex, "Transaction import preview rejected");
            TempData["Message"] = ex.Message;
            return RedirectToAction(nameof(TransactionsController.Index), "Transactions");
        }
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmImport([FromForm] string? csvContent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            TempData["Message"] = "La vista previa caducó. Vuelve a subir el archivo.";
            return RedirectToAction(nameof(TransactionsController.Index), "Transactions");
        }

        try
        {
            var imported = await sender.Send(new ConfirmTransactionImportCommand(csvContent), ct);
            TempData["Message"] = $"Se importaron {imported} movimientos.";
            return RedirectToAction(nameof(TransactionsController.Index), "Transactions");
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning(ex, "Transaction import confirmation rejected");
            TempData["Message"] = ex.Message;
            return RedirectToAction(nameof(TransactionsController.Index), "Transactions");
        }
    }
}
