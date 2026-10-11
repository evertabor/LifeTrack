using LifeTrack.Application.Identity;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record LoginViewModel
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public static LoginViewModel From(LoginInputModel input, string? errorMessage = null) => new()
    {
        Email = input.Email,
        Password = input.Password,
        ErrorMessage = errorMessage
    };

    public sealed record DisplayModel(string Email, string? ReturnUrl, string? ErrorMessage)
    {
        public static DisplayModel From(LoginResult result, string returnUrl) =>
            new(string.Empty, returnUrl, result.Succeeded ? null : result.ErrorMessage);
    }
}
