using LifeTrack.Application.Finance.Commands.ImportTransactions;

namespace LifeTrack.Web.ViewModels.Transactions;

public sealed record ImportPreviewViewModel
{
    public string CsvContent { get; init; } = string.Empty;
    public int TotalRows { get; init; }
    public int ValidCount { get; init; }
    public int InvalidCount { get; init; }
    public string SummaryText { get; init; } = string.Empty;
    public IReadOnlyList<ImportPreviewRowViewModel> ValidRows { get; init; } = [];
    public IReadOnlyList<ImportPreviewRowViewModel> InvalidRows { get; init; } = [];

    public bool HasValidRows => ValidRows.Count > 0;
    public bool HasInvalidRows => InvalidRows.Count > 0;

    public static ImportPreviewViewModel From(ImportPreviewResultDto result, string content) =>
        new()
        {
            CsvContent = content,
            TotalRows = result.TotalRows,
            ValidCount = result.ValidRows.Count,
            InvalidCount = result.InvalidRows.Count,
            SummaryText = $"{result.ValidRows.Count} válidas y {result.InvalidRows.Count} inválidas de {result.TotalRows} filas.",
            ValidRows = result.ValidRows.Select(ImportPreviewRowViewModel.From).ToList(),
            InvalidRows = result.InvalidRows.Select(ImportPreviewRowViewModel.From).ToList()
        };
}
