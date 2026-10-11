using Microsoft.AspNetCore.Identity;

namespace LifeTrack.Infrastructure.Persistence;

public class ApplicationUser : IdentityUser<Guid>
{
    public const int DisplayNameMaxLength = 100;
    public const int PhoneNumberMaxLength = 20;

    private ApplicationUser() { }

    public string? DisplayName { get; private set; }

    public static ApplicationUser Create(string email) =>
        new() { Email = email, UserName = email };

    public void UpdateProfile(string? displayName, string? phoneNumber)
    {
        DisplayName = Normalize(displayName);
        PhoneNumber = Normalize(phoneNumber);
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
