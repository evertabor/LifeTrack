using MediatR;

namespace LifeTrack.Application.Finance.Commands.UpdateTransaction;

public sealed record UpdateTransactionCommand(
    Guid Id,
    Guid CategoryId,
    bool IsIncome,
    decimal Amount,
    DateOnly Date,
    string? Description) : IRequest;
