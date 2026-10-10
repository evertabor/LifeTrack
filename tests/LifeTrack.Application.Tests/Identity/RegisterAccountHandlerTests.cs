using LifeTrack.Application.Identity;
using LifeTrack.Application.Tests.Fakes;

namespace LifeTrack.Application.Tests.Identity;

public class RegisterAccountHandlerTests
{
    [Fact]
    public async Task RegisterAccountHandler_ValidInput_ReturnsSuccess()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(true, null),
            new LoginOperationResult(false, "Irrelevant"));
        var handler = new RegisterAccountHandler(fakeService);
        var command = new RegisterAccountCommand("test@example.com", "VeryStrongPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task RegisterAccountHandler_DuplicateEmail_ReturnsDuplicateError()
    {
        var duplicateErrors = new[] { "Email 'test@example.com' is already taken." };
        var fakeService = new FakeAccountService(
            new AccountOperationResult(false, duplicateErrors),
            new LoginOperationResult(false, "Irrelevant"));
        var handler = new RegisterAccountHandler(fakeService);
        var command = new RegisterAccountCommand("test@example.com", "VeryStrongPassword123!");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors!, e => e.Contains("already", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisterAccountHandler_WeakPassword_ReturnsIdentityErrors()
    {
        var passwordErrors = new[] { "Password must be at least 12 characters." };
        var fakeService = new FakeAccountService(
            new AccountOperationResult(false, passwordErrors),
            new LoginOperationResult(false, "Irrelevant"));
        var handler = new RegisterAccountHandler(fakeService);
        var command = new RegisterAccountCommand("test@example.com", "weak");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors!, e => e.Contains("12", StringComparison.OrdinalIgnoreCase));
    }
}
