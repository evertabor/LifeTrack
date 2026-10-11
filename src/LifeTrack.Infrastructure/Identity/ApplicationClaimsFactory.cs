using System.Security.Claims;
using LifeTrack.Application.Identity;
using LifeTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LifeTrack.Infrastructure.Identity;

public sealed class ApplicationClaimsFactory(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityOptions> options) : UserClaimsPrincipalFactory<ApplicationUser>(userManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            identity.AddClaim(new Claim(UserClaimTypes.DisplayName, user.DisplayName));
        return identity;
    }
}
