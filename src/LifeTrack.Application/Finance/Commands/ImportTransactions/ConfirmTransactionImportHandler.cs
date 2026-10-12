using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.ImportTransactions;

public sealed class ConfirmTransactionImportHandler(
    ITransactionRepository transactions,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<ConfirmTransactionImportCommand, ImportConfirmationResult>
{
    public async Task<ImportConfirmationResult> Handle(ConfirmTransactionImportCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede confirmar una importación sin usuario autenticado.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new BusinessRuleException("El archivo está vacío.");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(request.Content) > TransactionCsvParser.MaxContentBytes)
        {
            throw new BusinessRuleException("El archivo supera el máximo de 1 MB.");
        }

        var rows = TransactionCsvParser.ParseRows(request.Content);

        if (rows.Count == 0 || rows.Count > TransactionCsvParser.MaxRows)
        {
            throw new BusinessRuleException("El archivo no contiene movimientos válidos para importar.");
        }

        var userId = currentUser.UserId.Value;
        var names = (await categories.ListAsync(userId, ct))
            .ToDictionary(c => c.Name.Trim(), c => c.Id, StringComparer.OrdinalIgnoreCase);
        var today = timeProvider.Today();

        var imported = 0;
        var skipped = 0;
        var lineNumber = 0;

        foreach (var fields in rows)
        {
            lineNumber++;
            var row = ImportRowValidator.Validate(new ImportRow(lineNumber, fields), names, userId, today);

            if (!row.IsValid || row.Date is null)
            {
                skipped++;
                continue;
            }

            if (!names.TryGetValue(row.CategoryName.Trim(), out var categoryId))
            {
                skipped++;
                continue;
            }

            try
            {
                await transactions.AddAsync(Transaction.Create(
                    userId,
                    new TransactionData(
                        categoryId,
                        row.IsIncome ? TransactionType.Income : TransactionType.Expense,
                        row.Amount,
                        row.Date.Value,
                        row.Description),
                    today), ct);
                imported++;
            }
            catch (DomainException)
            {
                skipped++;
                continue;
            }
        }

        if (imported == 0)
        {
            throw new BusinessRuleException("No hay filas válidas para importar.");
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new ImportConfirmationResult(imported, skipped);
    }
}
