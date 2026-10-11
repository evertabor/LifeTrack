using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record RegisterInputModel
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
}
