using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record LoginInputModel
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
