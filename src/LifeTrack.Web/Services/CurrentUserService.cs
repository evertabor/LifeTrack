using System.Security.Claims;
using LifeTrack.Application.Abstractions;

namespace LifeTrack.Web.Services;

public sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor) : ICurrentUser
{

    public Guid? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                ?? user.FindFirst("sub");
            if (claim is null)
                return null;
            return Guid.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
