using System.Security.Claims;
using LifeTrack.Web.Services;
using Microsoft.AspNetCore.Http;

namespace LifeTrack.Integration.Tests;

public class CurrentUserServiceTests
{
    private static CurrentUserService AuthenticatedService(string claimValue, string claimType)
    {
        var identity = new ClaimsIdentity([new Claim(claimType, claimValue)], "Test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new CurrentUserService(new HttpContextAccessor { HttpContext = context });
    }

    private static CurrentUserService AnonymousService()
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        return new CurrentUserService(new HttpContextAccessor { HttpContext = context });
    }

    [Fact]
    public void UserId_AuthenticatedPrincipal_ReturnsGuid()
    {
        var userId = Guid.NewGuid();

        var service = AuthenticatedService(userId.ToString(), ClaimTypes.NameIdentifier);

        Assert.Equal(userId, service.UserId);
    }

    [Fact]
    public void UserId_SubClaim_ReturnsGuid()
    {
        var userId = Guid.NewGuid();

        var service = AuthenticatedService(userId.ToString(), "sub");

        Assert.Equal(userId, service.UserId);
    }

    [Fact]
    public void UserId_AnonymousPrincipal_Throws()
    {
        var service = AnonymousService();

        Assert.Throws<InvalidOperationException>(() => service.UserId);
    }

    [Fact]
    public void UserId_NullHttpContext_Throws()
    {
        var service = new CurrentUserService(new HttpContextAccessor());

        Assert.Throws<InvalidOperationException>(() => service.UserId);
    }

    [Fact]
    public void UserId_MalformedClaim_Throws()
    {
        var service = AuthenticatedService("not-a-guid", ClaimTypes.NameIdentifier);

        Assert.Throws<InvalidOperationException>(() => service.UserId);
    }
}
