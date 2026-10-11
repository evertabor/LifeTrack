using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Common;
using MediatR;

namespace LifeTrack.Application.Identity;

public sealed class GetProfileHandler(
    IAccountService accountService,
    ICurrentUser currentUser) : IRequestHandler<GetProfileQuery, UserProfileDto?>
{
    public async Task<UserProfileDto?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            throw new BusinessRuleException("No se puede consultar el perfil sin usuario autenticado.");
        }

        var profile = await accountService.GetProfileAsync(currentUser.UserId.Value, cancellationToken);
        if (profile is null)
        {
            throw new ResourceNotFoundException("No se encontró el perfil.");
        }

        return profile;
    }
}
