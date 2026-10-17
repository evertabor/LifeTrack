using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.EditCategory;

public sealed class EditCategoryHandler(
    ICategoryRepository repository,
    ITransactionRepository transactions,
    IBudgetRepository budgets,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<EditCategoryCommand>
{
    private const string TypeBlockedMessage =
        "No se puede cambiar el tipo porque la categoría tiene movimientos o presupuestos asociados.";

    public async Task Handle(EditCategoryCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede editar una categoría sin usuario autenticado.");
        }

        var userId = currentUser.UserId.Value;
        var category = await repository.GetByIdAsync(request.Id, userId, ct);

        if (category is null)
        {
            throw new ResourceNotFoundException("La categoría no existe.");
        }

        try
        {
            var requestedType = request.IsIncome ? TransactionType.Income : TransactionType.Expense;

            if (requestedType != category.Type)
            {
                await EnsureTypeIsEditable(category, userId, ct);
            }

            await EnsureNameIsAvailable(request.Name, category, ct);
            category.ChangeType(requestedType);
            category.EditName(request.Name);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }

    private async Task EnsureNameIsAvailable(string? name, Category category, CancellationToken ct)
    {
        var normalizedName = name?.Trim() ?? string.Empty;

        if (normalizedName != category.Name
            && await repository.ExistsAsync(normalizedName, category.UserId, ct))
        {
            throw new DomainException("Ya existe una categoría con ese nombre.");
        }
    }

    private async Task EnsureTypeIsEditable(Category category, Guid userId, CancellationToken ct)
    {
        var hasTransactions = await transactions.ExistsForCategoryAsync(category.Id, userId, ct);
        var hasBudgets = await budgets.ExistsForCategoryAsync(category.Id, userId, ct);

        if (hasTransactions || hasBudgets)
        {
            throw new DomainException(TypeBlockedMessage);
        }
    }
}
