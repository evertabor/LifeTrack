using LifeTrack.Application.Common;

namespace LifeTrack.Application.Finance.Commands.ImportTransactions;

public sealed record PreviewTransactionsQuery(string Content) : IQuery<ImportPreviewResultDto>;
