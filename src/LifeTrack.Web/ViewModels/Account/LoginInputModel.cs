using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record LoginInputModel
{
    public const int EmailMaxLength = 256;
    public const int PasswordMaxLength = 256;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduce un correo electrónico válido.")]
    [StringLength(EmailMaxLength, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(PasswordMaxLength, ErrorMessage = "La contraseña no puede superar los 256 caracteres.")]
    public string Password { get; init; } = string.Empty;
}
