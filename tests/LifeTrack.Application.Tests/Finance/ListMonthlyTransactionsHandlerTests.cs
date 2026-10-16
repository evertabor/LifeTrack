using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Application.Tests.Finance.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class ListMonthlyTransactionsHandlerTests
{
    private static readonly DateOnly October15 = new(2026, 10, 15);

    private static async Task<(ListMonthlyTransactionsHandler Handler, Guid UserId, Guid FoodId, Guid TransportId)> ArrangeAsync()
    {
        var userId = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var food = Category.Create("Comida", userId, TransactionType.Expense);
        var transport = Category.Create("Transporte", userId, TransactionType.Expense);
        var salary = Category.Create("Sueldo", userId, TransactionType.Income);
        await categories.AddAsync(food, CancellationToken.None);
        await categories.AddAsync(transport, CancellationToken.None);
        await categories.AddAsync(salary, CancellationToken.None);
        var today = new DateOnly(2026, 10, 31);
        transactions.AddAsync(Transaction.Create(userId, new TransactionData(salary.Id, TransactionType.Income, 1000m, new DateOnly(2026, 10, 5), null), today, salary), CancellationToken.None).GetAwaiter().GetResult();
        transactions.AddAsync(Transaction.Create(userId, new TransactionData(food.Id, TransactionType.Expense, 250m, new DateOnly(2026, 10, 6), null), today, food), CancellationToken.None).GetAwaiter().GetResult();
        transactions.AddAsync(Transaction.Create(userId, new TransactionData(transport.Id, TransactionType.Expense, 150m, October15, null), today, transport), CancellationToken.None).GetAwaiter().GetResult();
        transactions.AddAsync(Transaction.Create(userId, new TransactionData(food.Id, TransactionType.Expense, 500m, new DateOnly(2026, 9, 20), null), today, food), CancellationToken.None).GetAwaiter().GetResult();
        var handler = new ListMonthlyTransactionsHandler(transactions, categories, new FakeCurrentUser(userId), new FixedTimeProvider(today));
        return (handler, userId, food.Id, transport.Id);
    }

    [Fact]
    public async Task Handle_OctoberMonth_ReturnsTotalsAndBalance()
    {
        var arranged = await ArrangeAsync();

        var result = await arranged.Handler.Handle(
            new ListMonthlyTransactionsQuery(2026, 10), CancellationToken.None);

        Assert.Equal(1000m, result.TotalIncome);
        Assert.Equal(400m, result.TotalExpense);
        Assert.Equal(600m, result.Balance);
        Assert.Equal(3, result.Transactions.Count);
    }

    [Fact]
    public async Task Handle_OctoberMonth_GroupsExpenseByCategory()
    {
        var arranged = await ArrangeAsync();

        var result = await arranged.Handler.Handle(
            new ListMonthlyTransactionsQuery(2026, 10), CancellationToken.None);

        Assert.Equal(2, result.ExpenseByCategory.Count);
        Assert.Equal(400m, result.ExpenseByCategory.Sum(c => c.Total));
    }

    [Fact]
    public async Task Handle_MonthWithoutTransactions_ReturnsZeros()
    {
        var arranged = await ArrangeAsync();

        var result = await arranged.Handler.Handle(
            new ListMonthlyTransactionsQuery(2026, 11), CancellationToken.None);

        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(0m, result.TotalExpense);
        Assert.Equal(0m, result.Balance);
        Assert.Empty(result.Transactions);
    }

    [Fact]
    public async Task Handle_FilterByCategory_ReturnsOnlyThatCategory()
    {
        var arranged = await ArrangeAsync();

        var result = await arranged.Handler.Handle(
            new ListMonthlyTransactionsQuery(2026, 10, arranged.TransportId), CancellationToken.None);

        Assert.Single(result.Transactions);
        Assert.Equal(150m, result.TotalExpense);
        Assert.Equal(0m, result.TotalIncome);
    }

    [Fact]
    public async Task Handle_CategoryOfAnotherUser_ThrowsResourceNotFound()
    {
        var arranged = await ArrangeAsync();
        var attacker = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var foreign = Category.Create("Comida", arranged.UserId, TransactionType.Expense);
        await categories.AddAsync(foreign, CancellationToken.None);
        var handler = new ListMonthlyTransactionsHandler(transactions, categories, new FakeCurrentUser(attacker), new FixedTimeProvider(new DateOnly(2026, 10, 31)));

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(new ListMonthlyTransactionsQuery(2026, 10, foreign.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidPeriod_ThrowsBusinessRule()
    {
        var arranged = await ArrangeAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(new ListMonthlyTransactionsQuery(2026, 13), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NullPeriod_DefaultsToCurrentMonth()
    {
        var userId = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var today = new DateOnly(2026, 10, 31);
        var category = Category.Create("Comida", userId, TransactionType.Expense);
        await transactions.AddAsync(
            Transaction.Create(userId, new TransactionData(category.Id, TransactionType.Expense, 25m, new DateOnly(2026, 10, 6), null), today, category),
            CancellationToken.None);
        var handler = new ListMonthlyTransactionsHandler(transactions, categories, new FakeCurrentUser(userId), new FixedTimeProvider(today));

        var result = await handler.Handle(new ListMonthlyTransactionsQuery(null, null), CancellationToken.None);

        Assert.Equal(2026, result.Year);
        Assert.Equal(10, result.Month);
        Assert.Single(result.Transactions);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var handler = new ListMonthlyTransactionsHandler(
            new FakeTransactionRepository(), new FakeCategoryRepository(), new FakeCurrentUser(null), new FixedTimeProvider(new DateOnly(2026, 10, 31)));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new ListMonthlyTransactionsQuery(2026, 10), CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}
