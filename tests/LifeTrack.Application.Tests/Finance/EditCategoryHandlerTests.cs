using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.EditCategory;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class EditCategoryHandlerTests
{
    [Fact]
    public async Task Handle_ValidName_UpdatesName()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var category = Category.Create("Comida", userId);
        await repository.AddAsync(category, CancellationToken.None);
        var handler = new EditCategoryHandler(repository, unitOfWork, currentUser);
        var command = new EditCategoryCommand(category.Id, "Transporte");

        await handler.Handle(command, CancellationToken.None);

        var categories = await repository.ListAsync(userId, CancellationToken.None);
        Assert.Equal("Transporte", Assert.Single(categories).Name);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_SameName_Succeeds()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var category = Category.Create("Comida", userId);
        await repository.AddAsync(category, CancellationToken.None);
        var handler = new EditCategoryHandler(repository, unitOfWork, currentUser);
        var command = new EditCategoryCommand(category.Id, "Comida");

        await handler.Handle(command, CancellationToken.None);

        var categories = await repository.ListAsync(userId, CancellationToken.None);
        Assert.Equal("Comida", Assert.Single(categories).Name);
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        await repository.AddAsync(Category.Create("Comida", userId), CancellationToken.None);
        var category = Category.Create("Transporte", userId);
        await repository.AddAsync(category, CancellationToken.None);
        var handler = new EditCategoryHandler(repository, unitOfWork, currentUser);
        var command = new EditCategoryCommand(category.Id, "Comida");

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Ya existe una categoría con ese nombre.", ex.Message);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_EmptyName_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var category = Category.Create("Comida", userId);
        await repository.AddAsync(category, CancellationToken.None);
        var handler = new EditCategoryHandler(repository, unitOfWork, currentUser);
        var command = new EditCategoryCommand(category.Id, "");

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("obligatorio", ex.Message);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_NameTooLong_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var category = Category.Create("Comida", userId);
        await repository.AddAsync(category, CancellationToken.None);
        var handler = new EditCategoryHandler(repository, unitOfWork, currentUser);
        var command = new EditCategoryCommand(category.Id, new string('a', 51));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("50", ex.Message);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_CategoryOfAnotherUser_ThrowsResourceNotFound()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userB);
        var category = Category.Create("Comida", userA);
        await repository.AddAsync(category, CancellationToken.None);
        var handler = new EditCategoryHandler(repository, unitOfWork, currentUser);
        var command = new EditCategoryCommand(category.Id, "Transporte");

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, unitOfWork.SaveCount);
        var categories = await repository.ListAsync(userA, CancellationToken.None);
        Assert.Equal("Comida", Assert.Single(categories).Name);
    }

    [Fact]
    public async Task Handle_MissingCategory_ThrowsResourceNotFound()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var handler = new EditCategoryHandler(repository, unitOfWork, currentUser);
        var command = new EditCategoryCommand(Guid.NewGuid(), "Transporte");

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(null);
        var handler = new EditCategoryHandler(repository, unitOfWork, currentUser);
        var command = new EditCategoryCommand(Guid.NewGuid(), "Transporte");

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}
