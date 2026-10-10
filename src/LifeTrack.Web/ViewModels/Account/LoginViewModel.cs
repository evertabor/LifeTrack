using System.ComponentModel.DataAnnotations;
using LifeTrack.Application.Identity;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public string? ReturnUrl { get; init; }

    public string? ErrorMessage { get; init; }

    public sealed record DisplayModel(string Email, string? ReturnUrl, string? ErrorMessage)
    {
        public static DisplayModel From(LoginResult result, string returnUrl) =>
            new(string.Empty, returnUrl, result.Succeeded ? null : result.ErrorMessage);
    }
}
