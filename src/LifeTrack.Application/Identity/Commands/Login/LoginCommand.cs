using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Identity;

public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResult>;
