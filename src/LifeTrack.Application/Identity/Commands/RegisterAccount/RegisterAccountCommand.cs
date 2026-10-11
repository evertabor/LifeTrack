using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Identity;

public sealed record RegisterAccountCommand(string Email, string Password, string? DisplayName, string? PhoneNumber) : ICommand<RegisterAccountResult>;
