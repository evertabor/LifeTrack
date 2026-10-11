using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.DeleteTransaction;
using LifeTrack.Application.Finance.Commands.UpdateTransaction;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Application.Tests.Finance.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class UpdateTransactionHandlerTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static (UpdateTransactionHandler Handler, FakeTransactionRepository Transactions, FakeUnitOfWork UnitOfWork, Guid UserId, Guid CategoryId, Transaction Movement) Arrange()
    {
        var owner = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var category = Category.Create("Comida", owner);
        categories.AddAsync(category, CancellationToken.None).GetAwaiter().GetResult();
        var movement = Transaction.Create(owner, new TransactionData(category.Id, TransactionType.Expense, 25.50m, Today, "Compra"), Today);
        transactions.AddAsync(movement, CancellationToken.None).GetAwaiter().GetResult();
        var handler = new UpdateTransactionHandler(transactions, categories, unitOfWork, new FakeCurrentUser(owner), new FixedTimeProvider(Today));
        return (handler, transactions, unitOfWork, owner, category.Id, movement);
    }

    [Fact]
    public async Task Handle_ValidEdit_UpdatesAndSaves()
    {
        var arranged = Arrange();
        var command = new UpdateTransactionCommand(arranged.Movement.Id, arranged.CategoryId, true, 100m, Today, "Sueldo");

        await arranged.Handler.Handle(command, CancellationToken.None);

        Assert.Equal(100m, arranged.Movement.Amount);
        Assert.Equal(TransactionType.Income, arranged.Movement.Type);
        Assert.Equal("Sueldo", arranged.Movement.Description);
        Assert.Equal(1, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_ZeroAmount_ThrowsBusinessRuleAndDoesNotSave()
    {
        var arranged = Arrange();
        var command = new UpdateTransactionCommand(arranged.Movement.Id, arranged.CategoryId, false, 0m, Today, null);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(command, CancellationToken.None));
        Assert.Equal(25.50m, arranged.Movement.Amount);
        Assert.Equal(0, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_FutureDate_ThrowsBusinessRuleAndDoesNotSave()
    {
        var arranged = Arrange();
        var command = new UpdateTransactionCommand(arranged.Movement.Id, arranged.CategoryId, false, 10m, Today.AddDays(1), null);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(command, CancellationToken.None));
        Assert.Equal(Today, arranged.Movement.Date);
        Assert.Equal(0, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_MovementOfAnotherUser_ThrowsResourceNotFoundAndDoesNotSave()
    {
        var arranged = Arrange();
        var attackerTransactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateTransactionHandler(
            attackerTransactions, categories, unitOfWork, new FakeCurrentUser(Guid.NewGuid()), new FixedTimeProvider(Today));
        var command = new UpdateTransactionCommand(arranged.Movement.Id, arranged.CategoryId, false, 10m, Today, null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(25.50m, arranged.Movement.Amount);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_CategoryOfAnotherUser_ThrowsResourceNotFoundAndDoesNotSave()
    {
        var arranged = Arrange();
        var foreign = Category.Create("Restaurantes", Guid.NewGuid());
        var categories = new FakeCategoryRepository();
        var other = Category.Create("Comida", arranged.UserId);
        await categories.AddAsync(other, CancellationToken.None);
        var handler = new UpdateTransactionHandler(
            arranged.Transactions, categories, arranged.UnitOfWork, new FakeCurrentUser(arranged.UserId), new FixedTimeProvider(Today));
        var command = new UpdateTransactionCommand(arranged.Movement.Id, foreign.Id, false, 10m, Today, null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(25.50m, arranged.Movement.Amount);
        Assert.Equal(0, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var handler = new UpdateTransactionHandler(
            new FakeTransactionRepository(), new FakeCategoryRepository(), new FakeUnitOfWork(), new FakeCurrentUser(null), new FixedTimeProvider(Today));
        var command = new UpdateTransactionCommand(Guid.NewGuid(), Guid.NewGuid(), false, 10m, Today, null);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}

public class DeleteTransactionHandlerTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);

    [Fact]
    public async Task Handle_ExistingMovement_RemovesIt()
    {
        var userId = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var movement = Transaction.Create(userId, new TransactionData(Guid.NewGuid(), TransactionType.Expense, 10m, Today, null), Today);
        await transactions.AddAsync(movement, CancellationToken.None);
        var handler = new DeleteTransactionHandler(transactions, unitOfWork, new FakeCurrentUser(userId));

        await handler.Handle(new DeleteTransactionCommand(movement.Id), CancellationToken.None);

        Assert.Empty(await transactions.ListByPeriodAsync(userId, Today.AddDays(-1), Today.AddDays(1), CancellationToken.None));
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_MovementOfAnotherUser_ThrowsResourceNotFoundAndKeepsIt()
    {
        var owner = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var movement = Transaction.Create(owner, new TransactionData(Guid.NewGuid(), TransactionType.Expense, 10m, Today, null), Today);
        await transactions.AddAsync(movement, CancellationToken.None);
        var handler = new DeleteTransactionHandler(transactions, unitOfWork, new FakeCurrentUser(Guid.NewGuid()));

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(new DeleteTransactionCommand(movement.Id), CancellationToken.None));
        Assert.Equal(0, unitOfWork.SaveCount);
        Assert.Single(await transactions.ListByPeriodAsync(owner, Today.AddDays(-1), Today.AddDays(1), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MissingMovement_ThrowsResourceNotFound()
    {
        var handler = new DeleteTransactionHandler(
            new FakeTransactionRepository(), new FakeUnitOfWork(), new FakeCurrentUser(Guid.NewGuid()));

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(new DeleteTransactionCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var handler = new DeleteTransactionHandler(
            new FakeTransactionRepository(), new FakeUnitOfWork(), new FakeCurrentUser(null));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new DeleteTransactionCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}
