using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.SetBudget;

public sealed class SetBudgetHandler(
    IBudgetRepository budgets,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<SetBudgetCommand>
{
    public async Task Handle(SetBudgetCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede definir un presupuesto sin usuario autenticado.");
        }

        try
        {
            var category = await categories.GetByIdAsync(request.CategoryId, currentUser.UserId.Value, ct);
            if (category is null)
            {
                throw new NotFoundException("Category not found.");
            }

            var existing = await budgets.GetAsync(request.CategoryId, currentUser.UserId.Value, request.Year, request.Month, ct);
            if (existing is null)
            {
                await budgets.AddAsync(Budget.Create(
                    currentUser.UserId.Value,
                    request.CategoryId,
                    new BudgetPeriod(request.Year, request.Month),
                    request.Limit,
                    category), ct);
            }
            else
            {
                existing.ChangeLimit(request.Limit);
            }

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
