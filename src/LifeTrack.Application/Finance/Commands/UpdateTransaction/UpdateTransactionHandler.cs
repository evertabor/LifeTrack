using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.UpdateTransaction;

public sealed class UpdateTransactionHandler(
    ITransactionRepository transactions,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<UpdateTransactionCommand>
{
    public async Task Handle(UpdateTransactionCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede editar un movimiento sin usuario autenticado.");
        }

        var transaction = await transactions.GetByIdAsync(request.Id, currentUser.UserId.Value, ct);

        if (transaction is null)
        {
            throw new ResourceNotFoundException("El movimiento no existe.");
        }

        try
        {
            var category = await categories.GetByIdAsync(request.CategoryId, currentUser.UserId.Value, ct);
            if (category is null)
            {
                throw new NotFoundException("Category not found.");
            }

            transaction.Update(
                request.CategoryId,
                request.IsIncome ? TransactionType.Income : TransactionType.Expense,
                request.Amount,
                request.Date,
                request.Description,
                timeProvider.Today());

            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
        catch (NotFoundException ex)
        {
            throw new ResourceNotFoundException(ex.Message);
        }
    }
}
