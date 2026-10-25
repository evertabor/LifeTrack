using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Queries.BalanceProjection;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Application.Tests.Finance.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class GetBalanceProjectionHandlerTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static (GetBalanceProjectionHandler Handler, FakeTransactionRepository Transactions, Guid UserId, Category IncomeCategory, Category ExpenseCategory) Arrange()
    {
        var owner = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var incomeCategory = Category.Create("Sueldo", owner, TransactionType.Income);
        var expenseCategory = Category.Create("Comida", owner, TransactionType.Expense);
        categories.AddAsync(incomeCategory, CancellationToken.None).GetAwaiter().GetResult();
        categories.AddAsync(expenseCategory, CancellationToken.None).GetAwaiter().GetResult();

        var handler = new GetBalanceProjectionHandler(
            transactions, new FakeCurrentUser(owner), new FixedTimeProvider(Today));

        return (handler, transactions, owner, incomeCategory, expenseCategory);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var handler = new GetBalanceProjectionHandler(
            new FakeTransactionRepository(), new FakeCurrentUser(null), new FixedTimeProvider(Today));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new GetBalanceProjectionQuery(2026, 10), CancellationToken.None));

        Assert.Contains("usuario autenticado", ex.Message);
    }

    [Fact]
    public async Task Handle_InvalidPeriod_ThrowsBusinessRuleException()
    {
        var arranged = Arrange();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(new GetBalanceProjectionQuery(2026, 13), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoTransactions_ReturnsZeroProjection()
    {
        var arranged = Arrange();

        var result = await arranged.Handler.Handle(new GetBalanceProjectionQuery(2026, 10), CancellationToken.None);

        Assert.Equal(0m, result.ProjectedBalance);
        Assert.Equal(10, result.DaysElapsed);
        Assert.Equal(DateTime.DaysInMonth(2026, 10) - 10, result.DaysRemaining);
        Assert.Equal(0m, result.DailyAverageExpense);
        Assert.False(result.IsNegative);
    }

    [Fact]
    public async Task Handle_IncomeOnly_ReturnsPositiveProjection()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.IncomeCategory.Id, TransactionType.Income, 1500m, today, null), today, arranged.IncomeCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetBalanceProjectionQuery(2026, 10), CancellationToken.None);

        Assert.Equal(1500m, result.ProjectedBalance);
        Assert.Equal(10, result.DaysElapsed);
        Assert.Equal(DateTime.DaysInMonth(2026, 10) - 10, result.DaysRemaining);
        Assert.Equal(0m, result.DailyAverageExpense);
        Assert.False(result.IsNegative);
    }

    [Fact]
    public async Task Handle_ExpenseOnly_ReturnsNegativeProjection()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.ExpenseCategory.Id, TransactionType.Expense, 500m, today, null), today, arranged.ExpenseCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetBalanceProjectionQuery(2026, 10), CancellationToken.None);

        var expectedDailyAverage = 500m / 10;
        var expectedProjected = -500m - (expectedDailyAverage * (DateTime.DaysInMonth(2026, 10) - 10));

        Assert.Equal(expectedProjected, result.ProjectedBalance);
        Assert.Equal(10, result.DaysElapsed);
        Assert.Equal(DateTime.DaysInMonth(2026, 10) - 10, result.DaysRemaining);
        Assert.Equal(expectedDailyAverage, result.DailyAverageExpense);
        Assert.True(result.IsNegative);
    }

    [Fact]
    public async Task Handle_BalancedIncomeAndExpense_ReturnsCorrectProjection()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.IncomeCategory.Id, TransactionType.Income, 1500m, today, null), today, arranged.IncomeCategory), CancellationToken.None);
        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.ExpenseCategory.Id, TransactionType.Expense, 500m, today, null), today, arranged.ExpenseCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetBalanceProjectionQuery(2026, 10), CancellationToken.None);

        var expectedDailyAverage = 500m / 10;
        var expectedProjected = 1500m - 500m - (expectedDailyAverage * (DateTime.DaysInMonth(2026, 10) - 10));

        Assert.Equal(expectedProjected, result.ProjectedBalance);
        Assert.Equal(10, result.DaysElapsed);
        Assert.Equal(DateTime.DaysInMonth(2026, 10) - 10, result.DaysRemaining);
        Assert.Equal(expectedDailyAverage, result.DailyAverageExpense);
        Assert.True(result.IsNegative);
    }

    [Fact]
    public async Task Handle_PastMonth_ReturnsFullMonthElapsed()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.ExpenseCategory.Id, TransactionType.Expense, 500m, new DateOnly(2026, 9, 15), null), today, arranged.ExpenseCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetBalanceProjectionQuery(2026, 9), CancellationToken.None);

        Assert.Equal(DateTime.DaysInMonth(2026, 9), result.DaysElapsed);
        Assert.Equal(0, result.DaysRemaining);
        var expectedDailyAverage = 500m / DateTime.DaysInMonth(2026, 9);
        Assert.Equal(expectedDailyAverage, result.DailyAverageExpense);
    }

    [Fact]
    public async Task Handle_FutureMonth_ThrowsBusinessRule()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.ExpenseCategory.Id, TransactionType.Expense, 500m, today, null), today, arranged.ExpenseCategory), CancellationToken.None);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(new GetBalanceProjectionQuery(2026, 11), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TransactionsOfAnotherUser_AreNotVisible()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        var otherUser = Guid.NewGuid();
        var otherCategory = Category.Create("Comida", otherUser, TransactionType.Expense);
        await new FakeCategoryRepository().AddAsync(otherCategory, CancellationToken.None);

        await arranged.Transactions.AddAsync(Transaction.Create(otherUser, new TransactionData(otherCategory.Id, TransactionType.Expense, 500m, today, null), today, otherCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetBalanceProjectionQuery(2026, 10), CancellationToken.None);

        Assert.Equal(0m, result.ProjectedBalance);
        Assert.Equal(0m, result.DailyAverageExpense);
    }
}
