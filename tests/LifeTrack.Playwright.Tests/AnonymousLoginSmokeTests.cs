using System.Runtime.ExceptionServices;
using Microsoft.Playwright;

namespace LifeTrack.Playwright.Tests;

public sealed class AnonymousLoginSmokeTests
{
    private const string TransactionsPath = "/transactions";
    private const string LoginPath = "/account/login";
    private const string GenericLoginErrorMessage = "Correo o contraseña no válidos.";

    [LocalSqlServerFact]
    public async Task Root_AnonymousVisitor_RendersLoginPage()
    {
        var fixture = new PlaywrightSqlServerFixture();
        await fixture.InitializeAsync();
        Exception? testFailure = null;

        try
        {
            await AssertLoginPageAsync(fixture);
        }
        catch (Exception exception)
        {
            testFailure = exception;
        }

        try
        {
            await fixture.DisposeAsync();
        }
        catch (Exception cleanupFailure) when (testFailure is not null)
        {
            throw new AggregateException("The UX smoke test and fixture cleanup both failed.", testFailure, cleanupFailure);
        }

        if (testFailure is not null)
            ExceptionDispatchInfo.Capture(testFailure).Throw();
    }

    private static async Task AssertLoginPageAsync(PlaywrightSqlServerFixture fixture)
    {
        await using var page = await fixture.Browser.NewPageAsync();
        var response = await page.GotoAsync(fixture.BaseAddress.ToString())
            ?? throw new InvalidOperationException("The anonymous root navigation returned no response.");

        Assert.Equal(200, response.Status);
        Assert.Equal(new Uri(fixture.BaseAddress, "/account/login").AbsoluteUri, page.Url);
        Assert.True(await page.GetByRole(AriaRole.Heading, new() { Name = "Iniciar sesión", Exact = true }).IsVisibleAsync());
        Assert.True(await page.GetByLabel("Correo electrónico").IsVisibleAsync());
        Assert.True(await page.GetByLabel("Contraseña", new() { Exact = true }).IsVisibleAsync());
        Assert.True(await page.GetByRole(AriaRole.Button, new() { Name = "Iniciar sesión", Exact = true }).IsVisibleAsync());
        Assert.True(await page.GetByRole(AriaRole.Link, new() { Name = "Crear cuenta", Exact = true }).IsVisibleAsync());

        var privateResponse = await page.GotoAsync(new Uri(fixture.BaseAddress, TransactionsPath).AbsoluteUri)
            ?? throw new InvalidOperationException("The anonymous private-page navigation returned no response.");
        Assert.Equal(200, privateResponse.Status);
        Assert.Equal(LoginPath, new Uri(page.Url).AbsolutePath, StringComparer.OrdinalIgnoreCase);

        var email = $"ux-missing-{Guid.NewGuid():N}@example.com";
        var password = $"NoAccount-{Guid.NewGuid():N}-9!";
        await page.GetByLabel("Correo electrónico").FillAsync(email);
        await page.GetByLabel("Contraseña", new() { Exact = true }).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Iniciar sesión", Exact = true }).ClickAsync();

        Assert.True(await page.GetByText(GenericLoginErrorMessage, new() { Exact = true }).IsVisibleAsync());
    }
}
