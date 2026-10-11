namespace LifeTrack.Application.Finance.Commands.ImportTransactions;

public sealed record ImportPreviewResultDto(
    int TotalRows,
    IReadOnlyList<ImportPreviewRowDto> ValidRows,
    IReadOnlyList<ImportPreviewRowDto> InvalidRows);
