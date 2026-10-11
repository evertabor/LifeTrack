using MediatR;

namespace LifeTrack.Application.Finance.Commands.DeleteTransaction;

public sealed record DeleteTransactionCommand(Guid Id) : IRequest;
