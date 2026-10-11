using System.Security.Claims;
using LifeTrack.Application.Identity;

namespace LifeTrack.Web.Services;

public sealed class UserBadgeService(IHttpContextAccessor httpContextAccessor)
{
    private const int MaxInitials = 2;
    private static readonly char[] LocalPartSeparators = ['.', '_', '-', '+', ' '];

    public string DisplayName => ResolveDisplayName();

    public string Initials => BuildInitials(ResolveDisplayName());

    private string ResolveDisplayName()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return string.Empty;
        return user.FindFirst(UserClaimTypes.DisplayName)?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? string.Empty;
    }

    private static string BuildInitials(string email)
    {
        var localPart = email.Split('@')[0];
        var parts = localPart.Split(LocalPartSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var initials = string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])).Take(MaxInitials));
        return initials.Length == 0 ? "?" : initials;
    }
}
