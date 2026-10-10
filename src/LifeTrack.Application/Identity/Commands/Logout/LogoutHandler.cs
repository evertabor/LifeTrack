using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Identity;

public sealed class LogoutHandler(
    IAccountService accountService) : IRequestHandler<LogoutCommand>
{

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await accountService.LogoutAsync(cancellationToken);
    }
}
