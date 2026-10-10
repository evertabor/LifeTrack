using LifeTrack.Application.Common;

namespace LifeTrack.Application.Finance.Commands.CreateTransaction;

public sealed record CreateTransactionCommand(
    Guid CategoryId,
    bool IsIncome,
    decimal Amount,
    DateOnly Date,
    string? Description) : ICommand<Guid>;
