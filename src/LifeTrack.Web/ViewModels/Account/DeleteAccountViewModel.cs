using System.ComponentModel.DataAnnotations;
using LifeTrack.Application.Identity;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record DeleteAccountViewModel
{
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Debes escribir ELIMINAR para confirmar.")]
    [Compare("ExpectedConfirmation", ErrorMessage = "La confirmación debe ser ELIMINAR en mayúsculas.")]
    public string? Confirmation { get; init; }

    public string ExpectedConfirmation => "ELIMINAR";

    public static DeleteAccountViewModel From(UserProfileDto profile) =>
        new()
        {
            Email = profile.Email
        };
}
