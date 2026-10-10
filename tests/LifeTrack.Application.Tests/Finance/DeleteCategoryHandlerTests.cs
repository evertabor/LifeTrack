using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.DeleteCategory;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class DeleteCategoryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingCategory_RemovesIt()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var category = Category.Create("Comida", userId);
        await repository.AddAsync(category, CancellationToken.None);
        var handler = new DeleteCategoryHandler(repository, unitOfWork, currentUser);
        var command = new DeleteCategoryCommand(category.Id);

        await handler.Handle(command, CancellationToken.None);

        var categories = await repository.ListAsync(userId, CancellationToken.None);
        Assert.Empty(categories);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_MissingCategory_ThrowsResourceNotFound()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId);
        var handler = new DeleteCategoryHandler(repository, unitOfWork, currentUser);
        var command = new DeleteCategoryCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
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
        var handler = new DeleteCategoryHandler(repository, unitOfWork, currentUser);
        var command = new DeleteCategoryCommand(category.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal(0, unitOfWork.SaveCount);
        var categories = await repository.ListAsync(userA, CancellationToken.None);
        Assert.Single(categories);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var repository = new Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(null);
        var handler = new DeleteCategoryHandler(repository, unitOfWork, currentUser);
        var command = new DeleteCategoryCommand(Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}
