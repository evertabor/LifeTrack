using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Queries.BalanceProjection;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Application.Tests.Finance.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class GetMonthlyComparisonHandlerTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static (GetMonthlyComparisonHandler Handler, FakeTransactionRepository Transactions, Guid UserId, Category IncomeCategory, Category ExpenseCategory) Arrange()
    {
        var owner = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var incomeCategory = Category.Create("Sueldo", owner, TransactionType.Income);
        var expenseCategory = Category.Create("Comida", owner, TransactionType.Expense);
        categories.AddAsync(incomeCategory, CancellationToken.None).GetAwaiter().GetResult();
        categories.AddAsync(expenseCategory, CancellationToken.None).GetAwaiter().GetResult();

        var handler = new GetMonthlyComparisonHandler(
            transactions, new FakeCurrentUser(owner), new FixedTimeProvider(Today));

        return (handler, transactions, owner, incomeCategory, expenseCategory);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var handler = new GetMonthlyComparisonHandler(
            new FakeTransactionRepository(), new FakeCurrentUser(null), new FixedTimeProvider(Today));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new GetMonthlyComparisonQuery(2026, 10), CancellationToken.None));

        Assert.Contains("usuario autenticado", ex.Message);
    }

    [Fact]
    public async Task Handle_InvalidPeriod_ThrowsBusinessRuleException()
    {
        var arranged = Arrange();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(new GetMonthlyComparisonQuery(2026, 13), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoTransactions_ReturnsZeroComparison()
    {
        var arranged = Arrange();

        var result = await arranged.Handler.Handle(new GetMonthlyComparisonQuery(2026, 10), CancellationToken.None);

        Assert.Equal(0m, result.PreviousMonthIncome);
        Assert.Equal(0m, result.PreviousMonthExpense);
        Assert.Equal(0m, result.IncomeChangePercent);
        Assert.Equal(0m, result.ExpenseChangePercent);
    }

    [Fact]
    public async Task Handle_CurrentMonthOnly_ReturnsCurrentVsZero()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.IncomeCategory.Id, TransactionType.Income, 1500m, today, null), today, arranged.IncomeCategory), CancellationToken.None);
        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.ExpenseCategory.Id, TransactionType.Expense, 500m, today, null), today, arranged.ExpenseCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetMonthlyComparisonQuery(2026, 10), CancellationToken.None);

        Assert.Equal(0m, result.PreviousMonthIncome);
        Assert.Equal(0m, result.PreviousMonthExpense);
        Assert.Equal(0m, result.IncomeChangePercent);
        Assert.Equal(0m, result.ExpenseChangePercent);
    }

    [Fact]
    public async Task Handle_PreviousMonthOnly_ReturnsZeroVsPrevious()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.IncomeCategory.Id, TransactionType.Income, 1000m, new DateOnly(2026, 9, 15), null), today, arranged.IncomeCategory), CancellationToken.None);
        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.ExpenseCategory.Id, TransactionType.Expense, 400m, new DateOnly(2026, 9, 20), null), today, arranged.ExpenseCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetMonthlyComparisonQuery(2026, 10), CancellationToken.None);

        Assert.Equal(1000m, result.PreviousMonthIncome);
        Assert.Equal(400m, result.PreviousMonthExpense);
        Assert.Equal(-100m, result.IncomeChangePercent);
        Assert.Equal(-100m, result.ExpenseChangePercent);
    }

    [Fact]
    public async Task Handle_IncomeIncreased_ReturnsPositivePercent()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.IncomeCategory.Id, TransactionType.Income, 1000m, new DateOnly(2026, 9, 15), null), today, arranged.IncomeCategory), CancellationToken.None);
        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.IncomeCategory.Id, TransactionType.Income, 1500m, today, null), today, arranged.IncomeCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetMonthlyComparisonQuery(2026, 10), CancellationToken.None);

        Assert.Equal(1000m, result.PreviousMonthIncome);
        Assert.Equal(50m, result.IncomeChangePercent);
    }

    [Fact]
    public async Task Handle_IncomeDecreased_ReturnsNegativePercent()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.IncomeCategory.Id, TransactionType.Income, 1500m, new DateOnly(2026, 9, 15), null), today, arranged.IncomeCategory), CancellationToken.None);
        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.IncomeCategory.Id, TransactionType.Income, 1000m, today, null), today, arranged.IncomeCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetMonthlyComparisonQuery(2026, 10), CancellationToken.None);

        Assert.Equal(1500m, result.PreviousMonthIncome);
        Assert.Equal(-33.33333333333333333333333333m, result.IncomeChangePercent);
    }

    [Fact]
    public async Task Handle_ExpenseIncreased_ReturnsPositivePercent()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.ExpenseCategory.Id, TransactionType.Expense, 400m, new DateOnly(2026, 9, 15), null), today, arranged.ExpenseCategory), CancellationToken.None);
        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.ExpenseCategory.Id, TransactionType.Expense, 600m, today, null), today, arranged.ExpenseCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetMonthlyComparisonQuery(2026, 10), CancellationToken.None);

        Assert.Equal(400m, result.PreviousMonthExpense);
        Assert.Equal(50m, result.ExpenseChangePercent);
    }

    [Fact]
    public async Task Handle_ExpenseDecreased_ReturnsNegativePercent()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.ExpenseCategory.Id, TransactionType.Expense, 600m, new DateOnly(2026, 9, 15), null), today, arranged.ExpenseCategory), CancellationToken.None);
        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.ExpenseCategory.Id, TransactionType.Expense, 400m, today, null), today, arranged.ExpenseCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetMonthlyComparisonQuery(2026, 10), CancellationToken.None);

        Assert.Equal(600m, result.PreviousMonthExpense);
        Assert.Equal(-33.33333333333333333333333333m, result.ExpenseChangePercent);
    }

    [Fact]
    public async Task Handle_TransactionsOfAnotherUser_AreNotVisible()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        var otherUser = Guid.NewGuid();
        var otherIncomeCategory = Category.Create("Sueldo", otherUser, TransactionType.Income);
        var otherExpenseCategory = Category.Create("Comida", otherUser, TransactionType.Expense);
        await new FakeCategoryRepository().AddAsync(otherIncomeCategory, CancellationToken.None);
        await new FakeCategoryRepository().AddAsync(otherExpenseCategory, CancellationToken.None);

        await arranged.Transactions.AddAsync(Transaction.Create(otherUser, new TransactionData(otherIncomeCategory.Id, TransactionType.Income, 1000m, new DateOnly(2026, 9, 15), null), today, otherIncomeCategory), CancellationToken.None);
        await arranged.Transactions.AddAsync(Transaction.Create(otherUser, new TransactionData(otherExpenseCategory.Id, TransactionType.Expense, 500m, today, null), today, otherExpenseCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetMonthlyComparisonQuery(2026, 10), CancellationToken.None);

        Assert.Equal(0m, result.PreviousMonthIncome);
        Assert.Equal(0m, result.PreviousMonthExpense);
        Assert.Equal(0m, result.IncomeChangePercent);
        Assert.Equal(0m, result.ExpenseChangePercent);
    }
}
