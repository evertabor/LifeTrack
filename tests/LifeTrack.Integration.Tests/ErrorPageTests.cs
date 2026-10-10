namespace LifeTrack.Integration.Tests;

public class ErrorPageTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ErrorPageTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ErrorPage_DoesNotLeakExceptionDetails_OnNonExistentRoute()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/this-route-does-not-exist-xyz123");

        var status = (int)response.StatusCode;
        Assert.True(status < 400, $"Got {status}");

        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack trace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at ", body);
        Assert.DoesNotContain("System.", body);
        Assert.DoesNotContain("Microsoft.", body);
        Assert.DoesNotContain("InnerException", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SQL", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Connection", body, StringComparison.OrdinalIgnoreCase);
    }
}
