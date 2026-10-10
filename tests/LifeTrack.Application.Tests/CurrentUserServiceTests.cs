using System.Security.Claims;
using LifeTrack.Application.Abstractions;
using LifeTrack.Application.Tests.Fakes;

namespace LifeTrack.Application.Tests;

public class CurrentUserServiceTests
{
    [Fact]
    public void CurrentUserService_AuthenticatedPrincipal_ReturnsGuid()
    {
        var userId = Guid.NewGuid();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var fake = new FakeCurrentUser(userId);

        Assert.Equal(userId, fake.UserId);
    }

    [Fact]
    public void CurrentUserService_AnonymousPrincipal_ReturnsNull()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        var fake = new FakeCurrentUser(null);

        Assert.Null(fake.UserId);
    }

    [Fact]
    public void CurrentUserService_NullHttpContextUser_ReturnsNull()
    {
        var fake = new FakeCurrentUser(null);

        Assert.Null(fake.UserId);
    }

    [Fact]
    public void CurrentUserService_MalformedNameIdentifierClaim_ReturnsNull()
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var fake = new FakeCurrentUser(null);

        Assert.Null(fake.UserId);
    }
}
