using LifeTrack.Application.Common;

namespace LifeTrack.Application.Finance.Commands.ImportTransactions;

public sealed record ConfirmTransactionImportCommand(string Content) : ICommand<int>;
