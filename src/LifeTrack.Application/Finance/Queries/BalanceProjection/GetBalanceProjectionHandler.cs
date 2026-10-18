using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Domain.Finance;
using MediatR;

namespace LifeTrack.Application.Finance.Queries.BalanceProjection;

public sealed class GetBalanceProjectionHandler(
    ITransactionRepository transactions,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<GetBalanceProjectionQuery, BalanceProjectionResult>
{
    public async Task<BalanceProjectionResult> Handle(GetBalanceProjectionQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede consultar la proyección sin usuario autenticado.");
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

        var items = await transactions.ListByPeriodAsync(userId, from, to, ct);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var daysElapsed = GetDaysElapsed(year, month, daysInMonth, today);
        var daysRemaining = daysInMonth - daysElapsed;

        var totalIncome = items.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var totalExpense = items.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var dailyAverageExpense = daysElapsed > 0 ? totalExpense / daysElapsed : 0m;
        var projectedBalance = totalIncome - totalExpense - (dailyAverageExpense * daysRemaining);

        return new BalanceProjectionResult(
            projectedBalance,
            daysElapsed,
            daysRemaining,
            dailyAverageExpense,
            projectedBalance < 0m);
    }

    private static int GetDaysElapsed(int year, int month, int daysInMonth, DateOnly today)
    {
        if (year < today.Year || (year == today.Year && month < today.Month))
        {
            return daysInMonth;
        }

        if (year == today.Year && month == today.Month)
        {
            return today.Day;
        }

        return 0;
    }
}
