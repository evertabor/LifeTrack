using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Queries.UnusualSpending;

public sealed class GetUnusualSpendingAlertsHandler(
    ITransactionRepository transactions,
    ICategoryRepository categories,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<GetUnusualSpendingAlertsQuery, IReadOnlyList<UnusualSpendingAlert>>
{
    private const decimal UnusualThreshold = 2.5m;
    private const int MinPreviousTransactions = 5;

    public async Task<IReadOnlyList<UnusualSpendingAlert>> Handle(GetUnusualSpendingAlertsQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede consultar alertas sin usuario autenticado.");
        }

        var today = timeProvider.Today();
        var year = request.Year ?? today.Year;
        var month = request.Month ?? today.Month;

        if (year is < Budget.MinYear or > Budget.MaxYear || month is < Budget.MinMonth or > Budget.MaxMonth)
        {
            throw new BusinessRuleException("El periodo solicitado no es válido.");
        }

        var userId = currentUser.UserId.Value;
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1);
        var currentMonthStart = from.AddMonths(-3);
        var currentMonthEnd = to;

        var currentItems = await transactions.ListByPeriodAsync(userId, from, to, ct);
        var previousItems = await transactions.ListByPeriodAsync(userId, currentMonthStart, currentMonthEnd, ct);
        var categoriesList = await categories.ListAsync(userId, ct);
        var categoryNames = categoriesList.ToDictionary(c => c.Id, c => c.Name);

        var alerts = new List<UnusualSpendingAlert>();

        foreach (var transaction in currentItems.Where(t => t.Type == TransactionType.Expense))
        {
            var previousCount = previousItems.Count(t => t.CategoryId == transaction.CategoryId && t.Id != transaction.Id);
            if (previousCount < MinPreviousTransactions)
            {
                continue;
            }

            var previousTotal = previousItems
                .Where(t => t.CategoryId == transaction.CategoryId && t.Id != transaction.Id)
                .Sum(t => t.Amount);
            var average = previousTotal / previousCount;

            if (average <= 0m)
            {
                continue;
            }

            var ratio = transaction.Amount / average;
            if (ratio >= UnusualThreshold)
            {
                alerts.Add(new UnusualSpendingAlert(
                    transaction.Id,
                    categoryNames.GetValueOrDefault(transaction.CategoryId, "Sin categoría"),
                    transaction.Amount,
                    average,
                    ratio));
            }
        }

        return alerts.OrderByDescending(a => a.Ratio).ToList();
    }
}
