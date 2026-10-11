using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.GetTransaction;

public sealed class GetTransactionHandler(
    ITransactionRepository transactions,
    ICategoryRepository categories,
    ICurrentUser currentUser) : IRequestHandler<GetTransactionQuery, TransactionSummary>
{
    public async Task<TransactionSummary> Handle(GetTransactionQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede consultar un movimiento sin usuario autenticado.");
        }

        var transaction = await transactions.GetByIdAsync(request.Id, currentUser.UserId.Value, ct);

        if (transaction is null)
        {
            throw new ResourceNotFoundException("El movimiento no existe.");
        }

        var names = (await categories.ListAsync(currentUser.UserId.Value, ct)).ToDictionary(c => c.Id, c => c.Name);
        var isIncome = transaction.Type == TransactionType.Income;

        return new TransactionSummary(
            transaction.Id,
            transaction.CategoryId,
            names.GetValueOrDefault(transaction.CategoryId, "Sin categoría"),
            isIncome,
            isIncome ? "Ingreso" : "Gasto",
            transaction.Amount,
            transaction.Date,
            transaction.Description);
    }
}
