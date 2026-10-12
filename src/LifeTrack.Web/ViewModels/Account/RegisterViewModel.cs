using LifeTrack.Application.Identity;

namespace LifeTrack.Web.ViewModels.Account;

public sealed record RegisterViewModel
{
    public string Email { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public string? PhoneNumber { get; init; }

    public string Password { get; init; } = string.Empty;

    public string ConfirmPassword { get; init; } = string.Empty;

    public string PasswordHelpText => $"Usa al menos {RegisterInputModel.MinPasswordLength} caracteres.";

    public string? ErrorMessage { get; init; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public static RegisterViewModel From(RegisterInputModel input, string? errorMessage = null) => new()
    {
        Email = input.Email,
        DisplayName = input.DisplayName,
        PhoneNumber = input.PhoneNumber,
        Password = input.Password,
        ConfirmPassword = input.ConfirmPassword,
        ErrorMessage = errorMessage
    };

    public sealed record DisplayModel(string Email, string? ErrorMessage)
    {
        public static DisplayModel From(RegisterAccountResult result) =>
            new(string.Empty,
                result.Succeeded ? null : string.Join(" ", result.Errors ?? Array.Empty<string>()));
    }
}
