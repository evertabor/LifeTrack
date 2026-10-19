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
        var daysInMonth = GetDaysInMonth(year, month);
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

    private static int GetDaysInMonth(int year, int month)
    {
        return month switch
        {
            1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
            4 or 6 or 9 or 11 => 30,
            2 => IsLeapYear(year) ? 29 : 28,
            _ => 0
        };
    }

    private static bool IsLeapYear(int year)
    {
        return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
    }
}
