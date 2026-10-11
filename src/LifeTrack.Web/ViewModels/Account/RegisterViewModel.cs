using System.ComponentModel.DataAnnotations;
using LifeTrack.Application.Identity;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record RegisterViewModel
{
    public const int MinPasswordLength = 12;
    public const int DisplayNameMaxLength = 100;
    public const int PhoneNumberMaxLength = 20;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [StringLength(DisplayNameMaxLength, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string? DisplayName { get; init; }

    [StringLength(PhoneNumberMaxLength, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string? PhoneNumber { get; init; }

    [Required]
    [MinLength(MinPasswordLength)]
    public string Password { get; init; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; init; } = string.Empty;

    public string PasswordHelpText => $"Usa al menos {MinPasswordLength} caracteres.";

    public string? ErrorMessage { get; init; }

    public sealed record DisplayModel(string Email, string? ErrorMessage)
    {
        public static DisplayModel From(RegisterAccountResult result) =>
            new(string.Empty,
                result.Succeeded ? null : string.Join(" ", result.Errors ?? Array.Empty<string>()));
    }
}
