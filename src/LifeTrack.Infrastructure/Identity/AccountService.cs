using LifeTrack.Application.Identity;
using LifeTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LifeTrack.Infrastructure.Identity;

public sealed class AccountService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IAccountService
{

    public async Task<AccountOperationResult> RegisterAsync(string email, string password, string? displayName, string? phoneNumber, CancellationToken cancellationToken)
    {
        var user = ApplicationUser.Create(email);
        user.UpdateProfile(displayName, phoneNumber);
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await signInManager.SignInAsync(user, isPersistent: false);
        }
        return new AccountOperationResult(
            result.Succeeded,
            result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<LoginOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return new LoginOperationResult(false, "Invalid email or password.");
        return new LoginOperationResult(result.Succeeded, result.Succeeded ? null : "Invalid email or password.");
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        await signInManager.SignOutAsync();
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
            return null;
        return new UserProfileDto(user.Email, user.DisplayName, user.PhoneNumber);
    }

    public async Task<AccountOperationResult> UpdateProfileAsync(Guid userId, string? displayName, string? phoneNumber, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return new AccountOperationResult(false, new[] { "No se encontró el perfil." });
        if (displayName?.Trim().Length > ApplicationUser.DisplayNameMaxLength)
            return new AccountOperationResult(false, new[] { $"El nombre no puede superar los {ApplicationUser.DisplayNameMaxLength} caracteres." });
        if (phoneNumber?.Trim().Length > ApplicationUser.PhoneNumberMaxLength)
            return new AccountOperationResult(false, new[] { $"El teléfono no puede superar los {ApplicationUser.PhoneNumberMaxLength} caracteres." });
        user.UpdateProfile(displayName, phoneNumber);
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            await signInManager.RefreshSignInAsync(user);
        }
        return new AccountOperationResult(
            result.Succeeded,
            result.Errors.Select(e => e.Description).ToArray());
    }
}
