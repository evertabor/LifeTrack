using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.CreateTransaction;

public sealed class CreateTransactionHandler(
    ITransactionRepository transactions,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<CreateTransactionCommand, Guid>
{
    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede registrar un movimiento sin usuario autenticado.");
        }

        try
        {
            var category = await categories.GetByIdAsync(request.CategoryId, currentUser.UserId.Value, ct);
            if (category is null)
            {
                throw new NotFoundException("Category not found.");
            }

            var transaction = Transaction.Create(
                currentUser.UserId.Value,
                request.CategoryId,
                request.IsIncome ? TransactionType.Income : TransactionType.Expense,
                request.Amount,
                request.Date,
                request.Description,
                timeProvider.Today());

            await transactions.AddAsync(transaction, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return transaction.Id;
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
