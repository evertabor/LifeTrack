using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Queries.BalanceProjection;

public sealed class GetMonthlyComparisonHandler(
    ITransactionRepository transactions,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<GetMonthlyComparisonQuery, MonthlyComparisonResult>
{
    public async Task<MonthlyComparisonResult> Handle(GetMonthlyComparisonQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede consultar la comparativa sin usuario autenticado.");
        }

        var today = timeProvider.Today();
        var year = request.Year ?? today.Year;
        var month = request.Month ?? today.Month;

        if (year is < Budget.MinYear or > Budget.MaxYear || month is < Budget.MinMonth or > Budget.MaxMonth)
        {
            throw new BusinessRuleException("El periodo solicitado no es válido.");
        }

        if (year > today.Year || (year == today.Year && month > today.Month))
        {
            throw new BusinessRuleException("No se puede consultar la comparativa de un periodo futuro.");
        }

        var userId = currentUser.UserId.Value;

        var currentFrom = new DateOnly(year, month, 1);
        var currentTo = currentFrom.AddMonths(1);

        var prev = currentFrom.AddMonths(-1);
        var prevFrom = new DateOnly(prev.Year, prev.Month, 1);
        var prevTo = prevFrom.AddMonths(1);

        var currentItems = await transactions.ListByPeriodAsync(userId, currentFrom, currentTo, ct);
        var prevItems = await transactions.ListByPeriodAsync(userId, prevFrom, prevTo, ct);

        var currentIncome = currentItems.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var currentExpense = currentItems.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var prevIncome = prevItems.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var prevExpense = prevItems.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var incomeChange = prevIncome > 0m ? (currentIncome - prevIncome) / prevIncome * 100m : 0m;
        var expenseChange = prevExpense > 0m ? (currentExpense - prevExpense) / prevExpense * 100m : 0m;

        return new MonthlyComparisonResult(prevIncome, prevExpense, incomeChange, expenseChange);
    }
}
