namespace LifeTrack.Application.Identity;

public sealed record LoginResult(bool Succeeded, string? ErrorMessage);
