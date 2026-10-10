using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Identity;

public sealed record RegisterAccountCommand(string Email, string Password) : ICommand<RegisterAccountResult>;
