using System.Security.Claims;
using LifeTrack.Application.Identity;
using LifeTrack.Web.Services;
using Microsoft.AspNetCore.Http;

namespace LifeTrack.Integration.Tests;

public class UserBadgeServiceTests
{
    [Fact]
    public void Initials_DottedEmail_ReturnsTwoUppercaseInitials()
    {
        var service = CreateService("ever.rodriguez@example.com");

        Assert.Equal("ER", service.Initials);
    }

    [Fact]
    public void Initials_SingleEmail_ReturnsOneInitial()
    {
        var service = CreateService("ever@example.com");

        Assert.Equal("E", service.Initials);
    }

    [Fact]
    public void Initials_Anonymous_ReturnsFallback()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var service = new UserBadgeService(accessor);

        Assert.Equal("?", service.Initials);
        Assert.Equal(string.Empty, service.DisplayName);
    }

    [Fact]
    public void DisplayName_Authenticated_ReturnsEmail()
    {
        var service = CreateService("ever@example.com");

        Assert.Equal("ever@example.com", service.DisplayName);
    }

    [Fact]
    public void DisplayName_WithDisplayNameClaim_ReturnsClaim()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Email, "ever@example.com"),
                new Claim(UserClaimTypes.DisplayName, "Ever Rodríguez")
            ],
            "test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var service = new UserBadgeService(new HttpContextAccessor { HttpContext = context });

        Assert.Equal("Ever Rodríguez", service.DisplayName);
        Assert.Equal("ER", service.Initials);
    }

    private static UserBadgeService CreateService(string email)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, email)],
            "test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = context };
        return new UserBadgeService(accessor);
    }
}
