using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.GetBudgetStatus;
using LifeTrack.Application.Finance.Commands.SetBudget;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Application.Tests.Finance.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class SetBudgetHandlerTests
{
    private static (SetBudgetHandler Handler, FakeBudgetRepository Budgets, FakeUnitOfWork UnitOfWork, Guid UserId, Guid CategoryId) Arrange()
    {
        var owner = Guid.NewGuid();
        var budgets = new FakeBudgetRepository();
        var categories = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var category = Category.Create("Comida", owner);
        categories.AddAsync(category, CancellationToken.None).GetAwaiter().GetResult();
        var handler = new SetBudgetHandler(budgets, categories, unitOfWork, new FakeCurrentUser(owner));
        return (handler, budgets, unitOfWork, owner, category.Id);
    }

    [Fact]
    public async Task Handle_ValidBudget_Saves()
    {
        var arranged = Arrange();

        await arranged.Handler.Handle(new SetBudgetCommand(arranged.CategoryId, 2026, 10, 100m), CancellationToken.None);

        Assert.Equal(1, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_TwiceForSamePeriod_UpdatesLimit()
    {
        var arranged = Arrange();
        await arranged.Handler.Handle(new SetBudgetCommand(arranged.CategoryId, 2026, 10, 100m), CancellationToken.None);

        await arranged.Handler.Handle(new SetBudgetCommand(arranged.CategoryId, 2026, 10, 150m), CancellationToken.None);

        var stored = await arranged.Budgets.GetAsync(arranged.CategoryId, arranged.UserId, 2026, 10, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal(150m, stored.Limit);
    }

    [Fact]
    public async Task Handle_ZeroLimit_ThrowsBusinessRuleAndDoesNotSave()
    {
        var arranged = Arrange();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(new SetBudgetCommand(arranged.CategoryId, 2026, 10, 0m), CancellationToken.None));
        Assert.Equal(0, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_CategoryOfAnotherUser_ThrowsResourceNotFoundAndDoesNotSave()
    {
        var arranged = Arrange();
        var foreign = Category.Create("Restaurantes", Guid.NewGuid());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => arranged.Handler.Handle(new SetBudgetCommand(foreign.Id, 2026, 10, 100m), CancellationToken.None));
        Assert.Equal(0, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var handler = new SetBudgetHandler(
            new FakeBudgetRepository(), new FakeCategoryRepository(), new FakeUnitOfWork(), new FakeCurrentUser(null));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new SetBudgetCommand(Guid.NewGuid(), 2026, 10, 100m), CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}

public class GetBudgetStatusHandlerTests
{
    private static async Task<(GetBudgetStatusHandler Handler, Guid UserId, Guid FoodId)> ArrangeAsync(decimal foodSpent)
    {
        var userId = Guid.NewGuid();
        var budgets = new FakeBudgetRepository();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var food = Category.Create("Comida", userId);
        await categories.AddAsync(food, CancellationToken.None);
        budgets.AddAsync(Budget.Create(userId, food.Id, new BudgetPeriod(2026, 10), 100m), CancellationToken.None).GetAwaiter().GetResult();
        var today = new DateOnly(2026, 10, 31);
        if (foodSpent > 0)
        {
            transactions.AddAsync(Transaction.Create(userId, new TransactionData(food.Id, TransactionType.Expense, foodSpent, new DateOnly(2026, 10, 6), null), today), CancellationToken.None).GetAwaiter().GetResult();
        }

        var handler = new GetBudgetStatusHandler(budgets, transactions, categories, new FakeCurrentUser(userId));
        return (handler, userId, food.Id);
    }

    [Fact]
    public async Task Handle_SpendingBelowWarning_ReturnsOk()
    {
        var arranged = await ArrangeAsync(50m);

        var result = await arranged.Handler.Handle(new GetBudgetStatusQuery(2026, 10), CancellationToken.None);

        var status = Assert.Single(result);
        Assert.Equal(50m, status.Spent);
        Assert.Equal(50m, status.Percent);
        Assert.Equal("ok", status.State);
    }

    [Fact]
    public async Task Handle_SpendingAt80Percent_ReturnsWarning()
    {
        var arranged = await ArrangeAsync(80m);

        var result = await arranged.Handler.Handle(new GetBudgetStatusQuery(2026, 10), CancellationToken.None);

        Assert.Equal("warning", Assert.Single(result).State);
    }

    [Fact]
    public async Task Handle_SpendingAboveLimit_ReturnsExceeded()
    {
        var arranged = await ArrangeAsync(120m);

        var result = await arranged.Handler.Handle(new GetBudgetStatusQuery(2026, 10), CancellationToken.None);

        var status = Assert.Single(result);
        Assert.Equal(120m, status.Percent);
        Assert.Equal("exceeded", status.State);
    }

    [Fact]
    public async Task Handle_NoBudgets_ReturnsEmpty()
    {
        var handler = new GetBudgetStatusHandler(
            new FakeBudgetRepository(), new FakeTransactionRepository(), new FakeCategoryRepository(), new FakeCurrentUser(Guid.NewGuid()));

        var result = await handler.Handle(new GetBudgetStatusQuery(2026, 10), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_InvalidPeriod_ThrowsBusinessRule()
    {
        var arranged = await ArrangeAsync(10m);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(new GetBudgetStatusQuery(2026, 13), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var handler = new GetBudgetStatusHandler(
            new FakeBudgetRepository(), new FakeTransactionRepository(), new FakeCategoryRepository(), new FakeCurrentUser(null));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new GetBudgetStatusQuery(2026, 10), CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}
