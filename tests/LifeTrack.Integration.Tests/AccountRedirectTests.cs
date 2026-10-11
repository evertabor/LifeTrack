using System.Text.RegularExpressions;
using LifeTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LifeTrack.Integration.Tests;

public class RedirectWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DbPath { get; } = Path.Combine(Path.GetTempPath(), $"lifetrack-redirect-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var removeTypes = new[]
            {
                typeof(DbContextOptions<AppDbContext>),
                typeof(DbContextOptions<>),
                typeof(AppDbContext)
            };

            foreach (var removeType in removeTypes)
            {
                var descriptors = services.Where(s => s.ServiceType == removeType).ToList();
                foreach (var d in descriptors)
                    services.Remove(d);
            }

            var internalProvider = new ServiceCollection()
                .AddEntityFrameworkSqlite()
                .BuildServiceProvider();
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite($"DataSource={DbPath}").UseInternalServiceProvider(internalProvider);
            var dbOptions = optionsBuilder.Options;
            services.AddScoped(_ => new AppDbContext(dbOptions));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            File.Delete(DbPath);
        }
        catch (IOException)
        {
        }
    }
}

public class AccountRedirectTests : IClassFixture<RedirectWebApplicationFactory>
{
    private const string Password = "TestPassword123!";

    private readonly RedirectWebApplicationFactory _factory;

    public AccountRedirectTests(RedirectWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ValidCredentials_RedirectsToTransactions()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await SeedUserAsync(email);
        using var client = CreateClient();

        var token = await ExtractAntiforgeryTokenAsync(client, "/Account/Login");
        using var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = Password,
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(302, (int)response.StatusCode);
        Assert.Equal("/transactions", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Login_ValidCredentialsWithReturnUrl_StillRedirectsToTransactions()
    {
        var email = $"login-return-{Guid.NewGuid():N}@example.com";
        await SeedUserAsync(email);
        using var client = CreateClient();

        var token = await ExtractAntiforgeryTokenAsync(client, "/Account/Login");
        using var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = Password,
            ["ReturnUrl"] = "/categories",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(302, (int)response.StatusCode);
        Assert.Equal("/transactions", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Register_ValidInput_RedirectsToTransactions()
    {
        using var client = CreateClient();

        await SeedDatabaseAsync();
        var token = await ExtractAntiforgeryTokenAsync(client, "/Account/Register");
        using var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = $"register-{Guid.NewGuid():N}@example.com",
            ["Password"] = Password,
            ["ConfirmPassword"] = Password,
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(302, (int)response.StatusCode);
        Assert.Equal("/transactions", response.Headers.Location?.ToString());
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    private async Task SeedUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var created = await users.CreateAsync(ApplicationUser.Create(email), Password);
        Assert.True(created.Succeeded, string.Join(" ", created.Errors.Select(e => e.Description)));
    }

    private async Task SeedDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    private static async Task<string> ExtractAntiforgeryTokenAsync(HttpClient client, string path)
    {
        using var page = await client.GetAsync(path);
        page.EnsureSuccessStatusCode();
        var match = Regex.Match(
            await page.Content.ReadAsStringAsync(),
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        Assert.True(match.Success, "Antiforgery token not found");
        return match.Groups[1].Value;
    }
}
