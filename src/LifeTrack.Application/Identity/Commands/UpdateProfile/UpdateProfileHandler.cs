using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Identity;

public sealed class UpdateProfileHandler(
    IAccountService accountService,
    ICurrentUser currentUser) : IRequestHandler<UpdateProfileCommand, AccountOperationResult>
{
    public async Task<AccountOperationResult> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede actualizar el perfil sin usuario autenticado.");
        }

        return await accountService.UpdateProfileAsync(currentUser.UserId.Value, request.DisplayName, request.PhoneNumber, cancellationToken);
    }
}
