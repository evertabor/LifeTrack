using LifeTrack.Application.Identity;

namespace LifeTrack.Application.Tests.Fakes;

public sealed class FakeAccountService : IAccountService
{
    public FakeAccountService(
        AccountOperationResult registerResult,
        LoginOperationResult loginResult,
        UserProfileDto? profileResult = null,
        AccountOperationResult? updateResult = null)
    {
        RegisterResult = registerResult;
        LoginResult = loginResult;
        ProfileResult = profileResult;
        UpdateResult = updateResult ?? new AccountOperationResult(true, null);
    }

    public AccountOperationResult RegisterResult { get; set; }
    public LoginOperationResult LoginResult { get; set; }
    public UserProfileDto? ProfileResult { get; set; }
    public AccountOperationResult UpdateResult { get; set; }
    public bool LogoutCalled { get; private set; }
    public (string Email, string Password, string? DisplayName, string? PhoneNumber)? LastRegister { get; private set; }
    public (Guid UserId, string? DisplayName, string? PhoneNumber)? LastUpdate { get; private set; }

    public Task<AccountOperationResult> RegisterAsync(string email, string password, string? displayName, string? phoneNumber, CancellationToken cancellationToken)
    {
        LastRegister = (email, password, displayName, phoneNumber);
        return Task.FromResult(RegisterResult);
    }

    public Task<LoginOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
        => Task.FromResult(LoginResult);

    public Task LogoutAsync(CancellationToken cancellationToken)
    {
        LogoutCalled = true;
        return Task.CompletedTask;
    }

    public Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
        => Task.FromResult(ProfileResult);

    public Task<AccountOperationResult> UpdateProfileAsync(Guid userId, string? displayName, string? phoneNumber, CancellationToken cancellationToken)
    {
        LastUpdate = (userId, displayName, phoneNumber);
        return Task.FromResult(UpdateResult);
    }
}
