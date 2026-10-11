using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record EditProfileInputModel
{
    public const int DisplayNameMaxLength = 100;
    public const int PhoneNumberMaxLength = 20;

    [StringLength(DisplayNameMaxLength, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string? DisplayName { get; init; }

    [StringLength(PhoneNumberMaxLength, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string? PhoneNumber { get; init; }
}
