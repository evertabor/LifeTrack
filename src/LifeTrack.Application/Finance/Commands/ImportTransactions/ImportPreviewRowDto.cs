namespace LifeTrack.Application.Finance.Commands.ImportTransactions;

public sealed record ImportPreviewRowDto(
    int LineNumber,
    DateOnly? Date,
    string CategoryName,
    bool IsIncome,
    string TypeLabel,
    decimal Amount,
    string Description,
    bool IsValid,
    string? Error);
