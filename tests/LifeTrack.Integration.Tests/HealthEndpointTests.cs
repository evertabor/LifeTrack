using System.Text.Json;

namespace LifeTrack.Integration.Tests;

public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_Returns200Ok_WithCorrectJson()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        var response = await client.GetAsync("/health");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("ok", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task HealthEndpoint_IsAccessibleWithoutAuthentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.False(response.Headers.Contains("Location"));
    }
}
