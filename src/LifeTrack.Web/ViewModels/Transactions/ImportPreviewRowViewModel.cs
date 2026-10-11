using System.Globalization;
using LifeTrack.Application.Finance.Commands.ImportTransactions;

namespace LifeTrack.Web.ViewModels.Transactions;

public sealed record ImportPreviewRowViewModel
{
    private static readonly CultureInfo Spanish = new("es-ES");

    public int LineNumber { get; init; }
    public string DateText { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string TypeLabel { get; init; } = string.Empty;
    public string AmountText { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public string Error { get; init; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(Error);

    public static ImportPreviewRowViewModel From(ImportPreviewRowDto row) =>
        new()
        {
            LineNumber = row.LineNumber,
            DateText = row.Date?.ToString("dd/MM/yyyy") ?? string.Empty,
            CategoryName = row.CategoryName,
            TypeLabel = row.TypeLabel,
            AmountText = row.Amount.ToString("N2", Spanish),
            Description = row.Description,
            IsValid = row.IsValid,
            Error = row.Error ?? string.Empty
        };
}
