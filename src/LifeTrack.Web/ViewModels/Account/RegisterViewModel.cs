using System.ComponentModel.DataAnnotations;
using LifeTrack.Application.Identity;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record RegisterViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(12)]
    public string Password { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public sealed record DisplayModel(string Email, string? ErrorMessage)
    {
        public static DisplayModel From(RegisterAccountResult result) =>
            new(string.Empty,
                result.Succeeded ? null : string.Join(" ", result.Errors ?? Array.Empty<string>()));
    }
}
