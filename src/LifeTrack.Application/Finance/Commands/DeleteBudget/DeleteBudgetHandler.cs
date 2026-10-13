using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.DeleteBudget;

public sealed class DeleteBudgetHandler(
    IBudgetRepository budgets,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<DeleteBudgetCommand>
{
    public async Task Handle(DeleteBudgetCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede eliminar un presupuesto sin usuario autenticado.");
        }

        var budget = await budgets.GetAsync(request.CategoryId, currentUser.UserId.Value, request.Year, request.Month, ct);

        if (budget is null)
        {
            throw new ResourceNotFoundException("El presupuesto no existe.");
        }

        budgets.Remove(budget);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
