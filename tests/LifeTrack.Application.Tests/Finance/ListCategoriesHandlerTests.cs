using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.ListCategories;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class ListCategoriesHandlerTests
{
    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyForUser()
    {
        var userId = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        var currentUser = new FakeCurrentUser(userId);
        var handler = new ListCategoriesHandler(repository, currentUser);

        var result = await handler.Handle(new ListCategoriesQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUserCategories()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        await repository.AddAsync(Category.Create("Comida", userA), CancellationToken.None);
        await repository.AddAsync(Category.Create("Transporte", userA), CancellationToken.None);
        await repository.AddAsync(Category.Create("Gastos", userB), CancellationToken.None);
        var currentUser = new FakeCurrentUser(userA);
        var handler = new ListCategoriesHandler(repository, currentUser);

        var result = await handler.Handle(new ListCategoriesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Contains(r.Name, new[] { "Comida", "Transporte" }));
    }

    [Fact]
    public async Task Handle_SameNameDifferentUsers_ReturnsOwnOnly()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var repository = new Fakes.FakeCategoryRepository();
        await repository.AddAsync(Category.Create("Comida", userA), CancellationToken.None);
        await repository.AddAsync(Category.Create("Comida", userB), CancellationToken.None);
        var currentUser = new FakeCurrentUser(userA);
        var handler = new ListCategoriesHandler(repository, currentUser);

        var result = await handler.Handle(new ListCategoriesQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Comida", result[0].Name);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var repository = new Fakes.FakeCategoryRepository();
        var currentUser = new FakeCurrentUser(null);
        var handler = new ListCategoriesHandler(repository, currentUser);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new ListCategoriesQuery(), CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}
