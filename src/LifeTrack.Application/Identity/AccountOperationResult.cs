namespace LifeTrack.Application.Identity;

public sealed record AccountOperationResult(bool Succeeded, string[]? Errors);
