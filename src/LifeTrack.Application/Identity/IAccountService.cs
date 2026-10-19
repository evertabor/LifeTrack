namespace LifeTrack.Application.Identity;

public interface IAccountService
{
    Task<AccountOperationResult> RegisterAsync(string email, string password, string? displayName, string? phoneNumber, CancellationToken cancellationToken);
    Task<LoginOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task LogoutAsync(CancellationToken cancellationToken);
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<AccountOperationResult> UpdateProfileAsync(Guid userId, string? displayName, string? phoneNumber, CancellationToken cancellationToken);
    Task<AccountOperationResult> DeleteAccountAsync(Guid userId, CancellationToken cancellationToken);
}
