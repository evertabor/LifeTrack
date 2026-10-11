using MediatR;

namespace LifeTrack.Application.Finance.Commands.SetBudget;

public sealed record SetBudgetCommand(
    Guid CategoryId,
    int Year,
    int Month,
    decimal Limit) : IRequest;
