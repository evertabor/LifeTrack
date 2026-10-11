using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.DeleteTransaction;

public sealed class DeleteTransactionHandler(
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<DeleteTransactionCommand>
{
    public async Task Handle(DeleteTransactionCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede eliminar un movimiento sin usuario autenticado.");
        }

        var transaction = await transactions.GetByIdAsync(request.Id, currentUser.UserId.Value, ct);

        if (transaction is null)
        {
            throw new ResourceNotFoundException("El movimiento no existe.");
        }

        await transactions.RemoveAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
