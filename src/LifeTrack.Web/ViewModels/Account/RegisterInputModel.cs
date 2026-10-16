using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record RegisterInputModel
{
    public const int MinPasswordLength = 12;
    public const int MaxPasswordLength = 256;
    public const int EmailMaxLength = 256;
    public const int DisplayNameMaxLength = 100;
    public const int PhoneNumberMaxLength = 20;
    public const string PhoneNumberPattern = @"\+?[0-9\s\-.()]*";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduce un correo electrónico válido.")]
    [StringLength(EmailMaxLength, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [StringLength(DisplayNameMaxLength, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string? DisplayName { get; init; }

    [StringLength(PhoneNumberMaxLength, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    [RegularExpression(PhoneNumberPattern, ErrorMessage = "El teléfono solo admite números, espacios y los signos + - . ( ).")]
    public string? PhoneNumber { get; init; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(MaxPasswordLength, MinimumLength = MinPasswordLength, ErrorMessage = "La contraseña debe tener entre 12 y 256 caracteres.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
