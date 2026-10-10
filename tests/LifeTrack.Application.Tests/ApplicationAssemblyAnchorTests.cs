using LifeTrack.Application.Common;

namespace LifeTrack.Application.Tests;

public class ApplicationAssemblyAnchorTests
{
    [Fact]
    public void ApplicationAssemblyAnchor_IsPublicAndSealed()
    {
        var anchor = typeof(ApplicationAssemblyAnchor);

        Assert.True(anchor.IsPublic);
        Assert.True(anchor.IsSealed);
    }
}
