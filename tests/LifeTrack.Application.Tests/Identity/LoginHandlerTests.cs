using LifeTrack.Application.Identity;
using LifeTrack.Application.Tests.Fakes;

namespace LifeTrack.Application.Tests.Identity;

public class LoginHandlerTests
{
    [Fact]
    public async Task LoginHandler_ValidCredentials_ReturnsSuccess()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(true, null),
            new LoginOperationResult(true, null));
        var handler = new LoginHandler(fakeService);
        var command = new LoginCommand("test@example.com", "VeryStrongPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task LoginHandler_WrongPassword_ReturnsGenericError()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(true, null),
            new LoginOperationResult(false, "Invalid email or password."));
        var handler = new LoginHandler(fakeService);
        var command = new LoginCommand("test@example.com", "WrongPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginHandler_NonExistentEmail_ReturnsGenericError()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(false, new[] { "Invalid email or password." }),
            new LoginOperationResult(false, "Invalid email or password."));
        var handler = new LoginHandler(fakeService);
        var command = new LoginCommand("nonexistent@example.com", "AnyPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginHandler_LockedOutAccount_ReturnsGenericError()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(true, null),
            new LoginOperationResult(false, "Invalid email or password."));
        var handler = new LoginHandler(fakeService);
        var command = new LoginCommand("locked@example.com", "AnyPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }
}
