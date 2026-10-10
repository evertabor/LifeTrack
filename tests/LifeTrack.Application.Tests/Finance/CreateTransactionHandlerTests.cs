using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.CreateTransaction;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Application.Tests.Finance.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class CreateTransactionHandlerTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static (CreateTransactionHandler Handler, FakeTransactionRepository Transactions, FakeUnitOfWork UnitOfWork, Guid UserId, Guid CategoryId) Arrange(Guid? userId = null)
    {
        var owner = userId ?? Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(owner);
        var timeProvider = new FixedTimeProvider(Today);
        var category = Category.Create("Comida", owner);
        categories.AddAsync(category, CancellationToken.None).GetAwaiter().GetResult();
        var handler = new CreateTransactionHandler(transactions, categories, unitOfWork, currentUser, timeProvider);
        return (handler, transactions, unitOfWork, owner, category.Id);
    }

    [Fact]
    public async Task Handle_ValidExpense_ReturnsIdAndSaves()
    {
        var arranged = Arrange();
        var command = new CreateTransactionCommand(arranged.CategoryId, false, 25.50m, Today, "Compra");

        var result = await arranged.Handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.Equal(1, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_ZeroAmount_ThrowsBusinessRuleAndDoesNotSave()
    {
        var arranged = Arrange();
        var command = new CreateTransactionCommand(arranged.CategoryId, false, 0m, Today, null);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_FutureDate_ThrowsBusinessRuleAndDoesNotSave()
    {
        var arranged = Arrange();
        var command = new CreateTransactionCommand(arranged.CategoryId, false, 10m, Today.AddDays(1), null);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_CategoryOfAnotherUser_ThrowsResourceNotFoundAndDoesNotSave()
    {
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var foreign = Category.Create("Comida", owner);
        await categories.AddAsync(foreign, CancellationToken.None);
        var handler = new CreateTransactionHandler(
            transactions, categories, unitOfWork, new FakeCurrentUser(attacker), new FixedTimeProvider(Today));
        var command = new CreateTransactionCommand(foreign.Id, false, 10m, Today, null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateTransactionHandler(
            transactions, categories, unitOfWork, new FakeCurrentUser(null), new FixedTimeProvider(Today));
        var command = new CreateTransactionCommand(Guid.NewGuid(), false, 10m, Today, null);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}
