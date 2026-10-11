using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;

public sealed class ListMonthlyTransactionsHandler(
    ITransactionRepository transactions,
    ICategoryRepository categories,
    ICurrentUser currentUser) : IRequestHandler<ListMonthlyTransactionsQuery, MonthlyTransactionsResult>
{
    public async Task<MonthlyTransactionsResult> Handle(ListMonthlyTransactionsQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede listar movimientos sin usuario autenticado.");
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

        var from = new DateOnly(request.Year, request.Month, 1);
        var items = await transactions.ListByPeriodAsync(userId, from, from.AddMonths(1), ct);

        if (request.CategoryId.HasValue)
        {
            items = items.Where(t => t.CategoryId == request.CategoryId.Value).ToList();
        }

        var names = (await categories.ListAsync(userId, ct)).ToDictionary(c => c.Id, c => c.Name);

        var summaries = items
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionSummary(
                t.Id,
                t.CategoryId,
                names.GetValueOrDefault(t.CategoryId, "Sin categoría"),
                t.Type == TransactionType.Income,
                t.Type == TransactionType.Income ? "Ingreso" : "Gasto",
                t.Amount,
                t.Date,
                t.Description))
            .ToList();

        var expenses = items.Where(t => t.Type == TransactionType.Expense).ToList();

        var byCategory = expenses
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryExpenseSummary(
                g.Key,
                names.GetValueOrDefault(g.Key, "Sin categoría"),
                g.Sum(t => t.Amount)))
            .OrderByDescending(g => g.Total)
            .ToList();

        return new MonthlyTransactionsResult(
            request.Year,
            request.Month,
            items.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            expenses.Sum(t => t.Amount),
            byCategory,
            summaries);
    }
}
