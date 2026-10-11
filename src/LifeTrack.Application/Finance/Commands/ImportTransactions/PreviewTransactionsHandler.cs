using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.ImportTransactions;

public sealed class PreviewTransactionsHandler(
    ICategoryRepository categories,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<PreviewTransactionsQuery, ImportPreviewResultDto>
{
    public async Task<ImportPreviewResultDto> Handle(PreviewTransactionsQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede previsualizar una importación sin usuario autenticado.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new BusinessRuleException("El archivo está vacío.");
        }

        var rows = TransactionCsvParser.ParseRows(request.Content);

        if (rows.Count == 0)
        {
            throw new BusinessRuleException("El archivo no contiene movimientos.");
        }

        if (rows.Count > TransactionCsvParser.MaxRows)
        {
            throw new BusinessRuleException($"El archivo supera el máximo de {TransactionCsvParser.MaxRows} filas.");
        }

        var userId = currentUser.UserId.Value;
        var names = (await categories.ListAsync(userId, ct))
            .ToDictionary(c => c.Name.Trim(), c => c.Id, StringComparer.OrdinalIgnoreCase);
        var today = timeProvider.Today();

        var valid = new List<ImportPreviewRowDto>();
        var invalid = new List<ImportPreviewRowDto>();
        var lineNumber = 0;

        foreach (var fields in rows)
        {
            lineNumber++;
            var row = ImportRowValidator.Validate(new ImportRow(lineNumber, fields), names, userId, today);

            if (row.IsValid)
            {
                valid.Add(row);
            }
            else
            {
                invalid.Add(row);
            }
        }

        return new ImportPreviewResultDto(rows.Count, valid, invalid);
    }
}
