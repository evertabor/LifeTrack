using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.GetBudgetStatus;

public sealed class GetBudgetStatusHandler(
    IBudgetRepository budgets,
    ITransactionRepository transactions,
    ICategoryRepository categories,
    ICurrentUser currentUser) : IRequestHandler<GetBudgetStatusQuery, IReadOnlyList<BudgetStatusDto>>
{
    public const decimal WarningPercent = 80m;
    public const decimal FullPercent = 100m;

    public async Task<IReadOnlyList<BudgetStatusDto>> Handle(GetBudgetStatusQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede consultar presupuestos sin usuario autenticado.");
        }

        if (request.Year is < Budget.MinYear or > Budget.MaxYear || request.Month is < Budget.MinMonth or > Budget.MaxMonth)
        {
            throw new BusinessRuleException("El periodo solicitado no es válido.");
        }

        var userId = currentUser.UserId.Value;

        if (request.CategoryId.HasValue)
        {
            var filter = await categories.GetByIdAsync(request.CategoryId.Value, userId, ct);
            if (filter is null)
            {
                throw new ResourceNotFoundException("La categoría no existe.");
            }
        }

        var items = await budgets.ListByPeriodAsync(userId, request.Year, request.Month, ct);

        if (request.CategoryId.HasValue)
        {
            items = items.Where(b => b.CategoryId == request.CategoryId.Value).ToList();
        }

        if (items.Count == 0)
        {
            return [];
        }

        var from = new DateOnly(request.Year, request.Month, 1);
        var movements = await transactions.ListByPeriodAsync(userId, from, from.AddMonths(1), ct);
        var names = (await categories.ListAsync(userId, ct)).ToDictionary(c => c.Id, c => c.Name);

        return items
            .OrderBy(b => names.GetValueOrDefault(b.CategoryId, "Sin categoría"))
            .Select(budget =>
            {
                var spent = movements
                    .Where(t => t.CategoryId == budget.CategoryId && t.Type == TransactionType.Expense)
                    .Sum(t => t.Amount);
                var percent = spent / budget.Limit * FullPercent;
                var state = percent >= FullPercent ? "exceeded" : percent >= WarningPercent ? "warning" : "ok";
                return new BudgetStatusDto(
                    budget.CategoryId,
                    names.GetValueOrDefault(budget.CategoryId, "Sin categoría"),
                    budget.Limit,
                    spent,
                    percent,
                    state);
            })
            .ToList();
    }
}
