using LifeTrack.Application.Identity;
using LifeTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LifeTrack.Infrastructure.Identity;

public sealed class AccountService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IAccountService
{

    public async Task<AccountOperationResult> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = ApplicationUser.Create(email);
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
}
