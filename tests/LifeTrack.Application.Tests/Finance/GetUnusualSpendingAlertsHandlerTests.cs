using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Queries.UnusualSpending;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Application.Tests.Finance.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class GetUnusualSpendingAlertsHandlerTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static (GetUnusualSpendingAlertsHandler Handler, FakeTransactionRepository Transactions, FakeCategoryRepository Categories, Guid UserId, Category Category) Arrange()
    {
        var owner = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var category = Category.Create("Comida", owner, TransactionType.Expense);
        categories.AddAsync(category, CancellationToken.None).GetAwaiter().GetResult();

        var handler = new GetUnusualSpendingAlertsHandler(
            transactions, categories, new FakeCurrentUser(owner), new FixedTimeProvider(Today));

        return (handler, transactions, categories, owner, category);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var handler = new GetUnusualSpendingAlertsHandler(
            new FakeTransactionRepository(), new FakeCategoryRepository(), new FakeCurrentUser(null), new FixedTimeProvider(Today));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new GetUnusualSpendingAlertsQuery(2026, 10), CancellationToken.None));

        Assert.Contains("usuario autenticado", ex.Message);
    }

    [Fact]
    public async Task Handle_InvalidPeriod_ThrowsBusinessRuleException()
    {
        var arranged = Arrange();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(new GetUnusualSpendingAlertsQuery(2026, 13), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotEnoughPreviousTransactions_ReturnsEmpty()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.Category.Id, TransactionType.Expense, 500m, today, null), today, arranged.Category), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetUnusualSpendingAlertsQuery(2026, 10), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_AverageZero_ReturnsEmpty()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        for (var i = 1; i <= 5; i++)
        {
            await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.Category.Id, TransactionType.Expense, 1m, today.AddDays(-i * 30), null), today, arranged.Category), CancellationToken.None);
        }

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.Category.Id, TransactionType.Expense, 500m, today, null), today, arranged.Category), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetUnusualSpendingAlertsQuery(2026, 10), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_BelowThreshold_ReturnsEmpty()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        for (var i = 1; i <= 5; i++)
        {
            await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.Category.Id, TransactionType.Expense, 100m, today.AddDays(-i * 30), null), today, arranged.Category), CancellationToken.None);
        }

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.Category.Id, TransactionType.Expense, 200m, today, null), today, arranged.Category), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetUnusualSpendingAlertsQuery(2026, 10), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_AboveThreshold_ReturnsAlert()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        for (var i = 1; i <= 5; i++)
        {
            await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.Category.Id, TransactionType.Expense, 100m, today.AddDays(-i * 10), null), today, arranged.Category), CancellationToken.None);
        }

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.Category.Id, TransactionType.Expense, 500m, today, null), today, arranged.Category), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetUnusualSpendingAlertsQuery(2026, 10), CancellationToken.None);

        var alert = Assert.Single(result);
        Assert.Equal(500m, alert.Amount);
        Assert.Equal(100m, alert.CategoryAverage);
        Assert.Equal(5.0m, alert.Ratio);
        Assert.Equal("Comida", alert.CategoryName);
    }

    [Fact]
    public async Task Handle_MultipleCategories_ReturnsOnlyUnusual()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        var transport = Category.Create("Transporte", arranged.UserId, TransactionType.Expense);
        await arranged.Categories.AddAsync(transport, CancellationToken.None);

        for (var i = 1; i <= 5; i++)
        {
            await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.Category.Id, TransactionType.Expense, 100m, today.AddDays(-i * 10), null), today, arranged.Category), CancellationToken.None);
            await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(transport.Id, TransactionType.Expense, 50m, today.AddDays(-i * 10), null), today, transport), CancellationToken.None);
        }

        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(arranged.Category.Id, TransactionType.Expense, 500m, today, null), today, arranged.Category), CancellationToken.None);
        await arranged.Transactions.AddAsync(Transaction.Create(arranged.UserId, new TransactionData(transport.Id, TransactionType.Expense, 60m, today, null), today, transport), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetUnusualSpendingAlertsQuery(2026, 10), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Comida", result[0].CategoryName);
    }

    [Fact]
    public async Task Handle_TransactionsOfAnotherUser_AreNotVisible()
    {
        var arranged = Arrange();
        var today = new DateOnly(2026, 10, 10);

        var otherUser = Guid.NewGuid();
        var otherCategory = Category.Create("Comida", otherUser, TransactionType.Expense);
        await arranged.Categories.AddAsync(otherCategory, CancellationToken.None);

        for (var i = 1; i <= 5; i++)
        {
            await arranged.Transactions.AddAsync(Transaction.Create(otherUser, new TransactionData(otherCategory.Id, TransactionType.Expense, 100m, today.AddDays(-i * 30), null), today, otherCategory), CancellationToken.None);
        }

        await arranged.Transactions.AddAsync(Transaction.Create(otherUser, new TransactionData(otherCategory.Id, TransactionType.Expense, 500m, today, null), today, otherCategory), CancellationToken.None);

        var result = await arranged.Handler.Handle(new GetUnusualSpendingAlertsQuery(2026, 10), CancellationToken.None);

        Assert.Empty(result);
    }
}
