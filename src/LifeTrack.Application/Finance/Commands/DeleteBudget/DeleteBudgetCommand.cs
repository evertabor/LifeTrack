using MediatR;

namespace LifeTrack.Application.Finance.Commands.DeleteBudget;

public sealed record DeleteBudgetCommand(Guid CategoryId, int Year, int Month) : IRequest;
