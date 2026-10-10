using LifeTrack.Application.Identity;
using LifeTrack.Application.Tests.Fakes;

namespace LifeTrack.Application.Tests.Identity;

public class LogoutHandlerTests
{
    [Fact]
    public async Task LogoutHandler_CallsLogoutAsync()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(true, null),
            new LoginOperationResult(true, null));
        var handler = new LogoutHandler(fakeService);
        var command = new LogoutCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.True(fakeService.LogoutCalled);
    }
}
