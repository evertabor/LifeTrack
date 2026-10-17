using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.CreateCategory;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class CreateCategoryHandlerTests
{
    [Fact]
    public async Task Handle_ValidName_ReturnsCategoryResult()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var handler = new CreateCategoryHandler(repository, unitOfWork, currentUser);
        var command = new CreateCategoryCommand("Comida", false);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Comida", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Handle_EmptyName_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var handler = new CreateCategoryHandler(repository, unitOfWork, currentUser);
        var command = new CreateCategoryCommand("", false);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("obligatorio", ex.Message);
    }

    [Fact]
    public async Task Handle_NameTooLong_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var handler = new CreateCategoryHandler(repository, unitOfWork, currentUser);
        var command = new CreateCategoryCommand(new string('a', 51), false);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("50", ex.Message);
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var existingCategory = Category.Create("Comida", userId, TransactionType.Expense);
        await repository.AddAsync(existingCategory, CancellationToken.None);
        var handler = new CreateCategoryHandler(repository, unitOfWork, currentUser);
        var command = new CreateCategoryCommand("  Comida  ", false);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Ya existe una categoría con ese nombre.", ex.Message);
    }

    [Fact]
    public async Task Handle_DuplicateDifferentCase_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var existingCategory = Category.Create("Comida", userId, TransactionType.Expense);
        await repository.AddAsync(existingCategory, CancellationToken.None);
        var handler = new CreateCategoryHandler(repository, unitOfWork, currentUser);
        var command = new CreateCategoryCommand("COMIDA", false);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Ya existe una categoría con ese nombre.", ex.Message);
    }

    [Fact]
    public async Task Handle_UsesCurrentUserId_NeverRequestId()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var handler = new CreateCategoryHandler(repository, unitOfWork, currentUser);
        var command = new CreateCategoryCommand("Transporte", false);

        var result = await handler.Handle(command, CancellationToken.None);

        var categories = await repository.ListAsync(userId, CancellationToken.None);
        Assert.Contains(categories, c => c.Id == result.Id && c.UserId == userId);
    }

    [Fact]
    public async Task Handle_DuplicateFromAnotherUser_Succeeds()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userB);
        var existingCategory = Category.Create("Comida", userA, TransactionType.Expense);
        await repository.AddAsync(existingCategory, CancellationToken.None);
        var handler = new CreateCategoryHandler(repository, unitOfWork, currentUser);
        var command = new CreateCategoryCommand("Comida", false);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Comida", result.Name);
    }

    [Fact]
    public async Task Handle_IncomeType_StoresIncomeCategory()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateCategoryHandler(repository, unitOfWork, new FakeCurrentUser(userId));
        var command = new CreateCategoryCommand("Sueldo", true);

        var result = await handler.Handle(command, CancellationToken.None);

        var categories = await repository.ListAsync(userId, CancellationToken.None);
        Assert.Contains(categories, c => c.Id == result.Id && c.Type == TransactionType.Income);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(null);
        var handler = new CreateCategoryHandler(repository, unitOfWork, currentUser);
        var command = new CreateCategoryCommand("Comida", false);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}
