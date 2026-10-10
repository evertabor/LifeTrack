using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.GetCategory;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class GetCategoryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingCategory_ReturnsSummary()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var currentUser = new FakeCurrentUser(userId);
        var category = Category.Create("Comida", userId);
        await repository.AddAsync(category, CancellationToken.None);
        var handler = new GetCategoryHandler(repository, currentUser);
        var query = new GetCategoryQuery(category.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(category.Id, result.Id);
        Assert.Equal("Comida", result.Name);
    }

    [Fact]
    public async Task Handle_MissingCategory_ThrowsResourceNotFound()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var currentUser = new FakeCurrentUser(userId);
        var handler = new GetCategoryHandler(repository, currentUser);
        var query = new GetCategoryQuery(Guid.NewGuid());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CategoryOfAnotherUser_ThrowsResourceNotFound()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var currentUser = new FakeCurrentUser(userB);
        var category = Category.Create("Comida", userA);
        await repository.AddAsync(category, CancellationToken.None);
        var handler = new GetCategoryHandler(repository, currentUser);
        var query = new GetCategoryQuery(category.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var repository = new Fakes.FakeCategoryRepository();
        var currentUser = new FakeCurrentUser(null);
        var handler = new GetCategoryHandler(repository, currentUser);
        var query = new GetCategoryQuery(Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(query, CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}
