namespace LifeTrack.Application.Identity;

public sealed record UserProfileDto(string Email, string? DisplayName, string? PhoneNumber);
