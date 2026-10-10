using LifeTrack.Application.Identity;

namespace LifeTrack.Application.Tests.Fakes;

public sealed class FakeAccountService : IAccountService
{
    public FakeAccountService(AccountOperationResult registerResult, LoginOperationResult loginResult)
    {
        RegisterResult = registerResult;
        LoginResult = loginResult;
    }

    public AccountOperationResult RegisterResult { get; set; }
    public LoginOperationResult LoginResult { get; set; }
    public bool LogoutCalled { get; private set; }

    public Task<AccountOperationResult> RegisterAsync(string email, string password, CancellationToken cancellationToken)
        => Task.FromResult(RegisterResult);

    public Task<LoginOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
        => Task.FromResult(LoginResult);

    public Task LogoutAsync(CancellationToken cancellationToken)
    {
        LogoutCalled = true;
        return Task.CompletedTask;
    }
}
