using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using LifeTrack.Application.Identity;
using MediatR;

namespace LifeTrack.Application.Identity;

public sealed class DeleteAccountHandler(
    IAccountService accountService,
    ICurrentUser currentUser) : IRequestHandler<DeleteAccountCommand>
{
    public async Task Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede eliminar la cuenta sin usuario autenticado.");
        }

        var result = await accountService.DeleteAccountAsync(currentUser.UserId.Value, ct);

        if (!result.Succeeded)
        {
            throw new BusinessRuleException(string.Join("; ", result.Errors ?? ["No se pudo eliminar la cuenta."]));
        }
    }
}
