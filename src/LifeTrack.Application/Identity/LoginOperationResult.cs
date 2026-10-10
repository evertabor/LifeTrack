namespace LifeTrack.Application.Identity;

public sealed record LoginOperationResult(bool Succeeded, string? ErrorMessage);
