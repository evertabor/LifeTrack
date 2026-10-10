using Microsoft.AspNetCore.Identity;

namespace LifeTrack.Infrastructure.Persistence;

public class ApplicationUser : IdentityUser<Guid>
{
    private ApplicationUser() { }

    public static ApplicationUser Create(string email) =>
        new() { Email = email, UserName = email };
}
