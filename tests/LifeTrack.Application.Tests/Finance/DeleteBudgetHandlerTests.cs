using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.DeleteBudget;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class DeleteBudgetHandlerTests
{
    [Fact]
    public async Task Handle_ExistingBudget_RemovesIt()
    {
        var userId = Guid.NewGuid();
        var budgets = new Fakes.FakeBudgetRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var categoryId = Guid.NewGuid();
        await budgets.AddAsync(Budget.Create(userId, categoryId, new BudgetPeriod(2026, 10), 100m), CancellationToken.None);
        var handler = new DeleteBudgetHandler(budgets, unitOfWork, currentUser);
        var command = new DeleteBudgetCommand(categoryId, 2026, 10);

        await handler.Handle(command, CancellationToken.None);

        var remaining = await budgets.ListByPeriodAsync(userId, 2026, 10, CancellationToken.None);
        Assert.Empty(remaining);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_MissingBudget_ThrowsResourceNotFound()
    {
        var userId = Guid.NewGuid();
        var budgets = new Fakes.FakeBudgetRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var handler = new DeleteBudgetHandler(budgets, unitOfWork, currentUser);
        var command = new DeleteBudgetCommand(Guid.NewGuid(), 2026, 10);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_BudgetOfAnotherUser_ThrowsResourceNotFound()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var budgets = new Fakes.FakeBudgetRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userB);
        var categoryId = Guid.NewGuid();
        await budgets.AddAsync(Budget.Create(userA, categoryId, new BudgetPeriod(2026, 10), 100m), CancellationToken.None);
        var handler = new DeleteBudgetHandler(budgets, unitOfWork, currentUser);
        var command = new DeleteBudgetCommand(categoryId, 2026, 10);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, unitOfWork.SaveCount);
        var remaining = await budgets.ListByPeriodAsync(userA, 2026, 10, CancellationToken.None);
        Assert.Single(remaining);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var budgets = new Fakes.FakeBudgetRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(null);
        var handler = new DeleteBudgetHandler(budgets, unitOfWork, currentUser);
        var command = new DeleteBudgetCommand(Guid.NewGuid(), 2026, 10);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}
