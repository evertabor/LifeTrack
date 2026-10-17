using System.Globalization;
using LifeTrack.Application.Abstractions;
using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Finance.Commands.ImportTransactions;

public sealed record ImportRow(int LineNumber, string[] Fields);

public static class ImportRowValidator
{
    private static readonly CultureInfo Spanish = new("es-ES");

    private const int ColumnCount = 5;

    public static ImportPreviewRowDto Validate(
        ImportRow row,
        IReadOnlyDictionary<string, Category> categoriesByName,
        Guid userId,
        DateOnly today)
    {
        var lineNumber = row.LineNumber;
        var fields = row.Fields;
        if (fields.Length != ColumnCount)
        {
            return Invalid(lineNumber, $"La fila debe tener {ColumnCount} columnas: fecha, categoría, tipo, importe y descripción.");
        }

        if (!TryParseDate(fields[0], out var date))
        {
            return Invalid(lineNumber, "La fecha no es válida.");
        }

        var categoryName = fields[1].Trim();
        if (!categoriesByName.TryGetValue(categoryName, out var category))
        {
            return Invalid(lineNumber, "La categoría no es válida.");
        }

        if (!TryParseType(fields[2], out var isIncome))
        {
            return Invalid(lineNumber, "El tipo debe ser Ingreso o Gasto.");
        }

        if (!TryParseAmount(fields[3], out var amount))
        {
            return Invalid(lineNumber, "El importe no es válido.");
        }

        try
        {
            var movement = Transaction.Create(
                userId,
                new TransactionData(
                    category.Id,
                    isIncome ? TransactionType.Income : TransactionType.Expense,
                    amount,
                    date,
                    fields[4]),
                today,
                category);

            return new ImportPreviewRowDto(
                lineNumber,
                movement.Date,
                categoryName,
                isIncome,
                isIncome ? "Ingreso" : "Gasto",
                movement.Amount,
                movement.Description,
                true,
                null);
        }
        catch (DomainException ex)
        {
            return Invalid(lineNumber, ex.Message);
        }
    }

    private static ImportPreviewRowDto Invalid(int lineNumber, string error) =>
        new(lineNumber, null, string.Empty, false, string.Empty, 0m, string.Empty, false, error);

    private static bool TryParseDate(string text, out DateOnly date)
    {
        var trimmed = text.Trim();
        return DateOnly.TryParse(trimmed, Spanish, DateTimeStyles.None, out date)
            || DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryParseType(string text, out bool isIncome)
    {
        if (text.Trim().Equals("Ingreso", StringComparison.OrdinalIgnoreCase))
        {
            isIncome = true;
            return true;
        }

        if (text.Trim().Equals("Gasto", StringComparison.OrdinalIgnoreCase))
        {
            isIncome = false;
            return true;
        }

        isIncome = false;
        return false;
    }

    private static bool TryParseAmount(string text, out decimal amount)
    {
        var trimmed = text.Trim();
        return decimal.TryParse(trimmed, NumberStyles.Number, Spanish, out amount)
            || decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
}
