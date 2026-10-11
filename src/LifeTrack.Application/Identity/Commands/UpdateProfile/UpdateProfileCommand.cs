using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Identity;

public sealed record UpdateProfileCommand(string? DisplayName, string? PhoneNumber) : ICommand<AccountOperationResult>;
