using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LifeTrack.Integration.Tests;

public class AntiforgeryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AntiforgeryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_FormContainsAntiforgeryToken()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");

        using var getResponse = await client.GetAsync("/Account/Register");

        var status = (int)getResponse.StatusCode;
        Assert.True(status < 400, $"GET /Account/Register returned {status}");

        var content = await getResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(content, @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        Assert.True(tokenMatch.Success, "Anti-forgery token not found on Register page");
        Assert.False(string.IsNullOrEmpty(tokenMatch.Groups[1].Value), "Anti-forgery token value is empty");
    }

    [Fact]
    public async Task Login_FormContainsAntiforgeryToken()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");

        using var getResponse = await client.GetAsync("/Account/Login");

        var status = (int)getResponse.StatusCode;
        Assert.True(status < 400, $"GET /Account/Login returned {status}");

        var content = await getResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(content, @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        Assert.True(tokenMatch.Success, "Anti-forgery token not found on Login page");
        Assert.False(string.IsNullOrEmpty(tokenMatch.Groups[1].Value), "Anti-forgery token value is empty");
    }

    [Fact]
    public async Task Register_PostWithoutAntiforgeryToken_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        using var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "user@example.com",
            ["Password"] = "ValidPassword123!"
        }));

        Assert.Equal(400, (int)response.StatusCode);
    }

    [Fact]
    public async Task Login_PostWithoutAntiforgeryToken_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        using var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "user@example.com",
            ["Password"] = "ValidPassword123!"
        }));

        Assert.Equal(400, (int)response.StatusCode);
    }

    [Fact]
    public async Task Logout_PostWithoutAuthentication_RedirectsToLogin()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        using var response = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()));

        Assert.Equal(302, (int)response.StatusCode);
        Assert.Contains("account/login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
