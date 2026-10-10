namespace LifeTrack.Application.Identity;

public sealed record RegisterAccountResult(bool Succeeded, string[]? Errors);
