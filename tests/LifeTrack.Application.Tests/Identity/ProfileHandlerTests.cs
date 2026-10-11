using LifeTrack.Application.Common;
using LifeTrack.Application.Identity;
using LifeTrack.Application.Tests.Fakes;

namespace LifeTrack.Application.Tests.Identity;

public class GetProfileHandlerTests
{
    [Fact]
    public async Task GetProfileHandler_Authenticated_ReturnsProfile()
    {
        var userId = Guid.NewGuid();
        var fakeService = new FakeAccountService(
            new AccountOperationResult(false, null),
            new LoginOperationResult(false, "Irrelevant"),
            new UserProfileDto("test@example.com", "Test User", null));
        var handler = new GetProfileHandler(fakeService, new FakeCurrentUser(userId));

        var result = await handler.Handle(new GetProfileQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
    }

    [Fact]
    public async Task GetProfileHandler_UnknownUser_ThrowsNotFound()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(false, null),
            new LoginOperationResult(false, "Irrelevant"));
        var handler = new GetProfileHandler(fakeService, new FakeCurrentUser(Guid.NewGuid()));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => handler.Handle(new GetProfileQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task GetProfileHandler_Anonymous_ThrowsBusinessRule()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(false, null),
            new LoginOperationResult(false, "Irrelevant"));
        var handler = new GetProfileHandler(fakeService, new FakeCurrentUser(null));

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(new GetProfileQuery(), CancellationToken.None));
    }
}

public class UpdateProfileHandlerTests
{
    [Fact]
    public async Task UpdateProfileHandler_ValidInput_ForwardsFields()
    {
        var userId = Guid.NewGuid();
        var fakeService = new FakeAccountService(
            new AccountOperationResult(false, null),
            new LoginOperationResult(false, "Irrelevant"));
        var handler = new UpdateProfileHandler(fakeService, new FakeCurrentUser(userId));

        var result = await handler.Handle(new UpdateProfileCommand("Test User", "600123456"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal((userId, "Test User", "600123456"), fakeService.LastUpdate);
    }

    [Fact]
    public async Task UpdateProfileHandler_Anonymous_ThrowsBusinessRule()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(false, null),
            new LoginOperationResult(false, "Irrelevant"));
        var handler = new UpdateProfileHandler(fakeService, new FakeCurrentUser(null));

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(new UpdateProfileCommand("Test User", null), CancellationToken.None));
        Assert.Null(fakeService.LastUpdate);
    }
}
