using LifeTrack.Application.Identity;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record ProfileViewModel
{
    public string Email { get; init; } = string.Empty;
    public EditProfileInputModel Input { get; init; } = new();
    public string? SuccessMessage { get; init; }

    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);

    public static ProfileViewModel From(UserProfileDto profile, EditProfileInputModel? input = null, string? message = null) =>
        new()
        {
            Email = profile.Email,
            Input = input ?? new() { DisplayName = profile.DisplayName, PhoneNumber = profile.PhoneNumber },
            SuccessMessage = message
        };
}
