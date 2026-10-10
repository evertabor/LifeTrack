namespace LifeTrack.Application.Identity;

public interface IAccountService
{
    Task<AccountOperationResult> RegisterAsync(string email, string password, CancellationToken cancellationToken);
    Task<LoginOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task LogoutAsync(CancellationToken cancellationToken);
}
