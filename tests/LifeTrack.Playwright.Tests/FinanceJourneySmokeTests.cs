using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Microsoft.Playwright;

namespace LifeTrack.Playwright.Tests;

public sealed class FinanceJourneySmokeTests
{
    private const string CategoryCreateButtonLabel = "Crear categoría";
    private const string CategoryCreateModalSelector = "#category-create-modal";
    private const string CategoryNameLabel = "Nombre";
    private const string CategoryTypeLabel = "Tipo";
    private const string CategoryTypeFilterLabel = "Filtrar por tipo";
    private const string CategoryIncomeTypeValue = "1";
    private const string CategoryExpenseTypeValue = "2";
    private const string DuplicateCategoryMessage = "Ya existe una categoría con ese nombre.";
    private const string TransactionCreateModalSelector = "#transaction-create-modal";
    private const string TransactionCreateButtonLabel = "Registrar movimiento";
    private const string TransactionTypeLabel = "Tipo";
    private const string TransactionCategoryLabel = "Categoría";
    private const string TransactionAmountLabel = "Importe";
    private const string TransactionDateLabel = "Fecha";
    private const string TransactionDescriptionLabel = "Descripción";
    private const string InvalidAmountMessage = "El importe debe ser mayor que 0 y no superar 1.000.000.000.";
    private const string IncomeTypeValue = "true";
    private const string ExpenseTypeValue = "false";
    private const string InvalidExpenseAmount = "0,00";
    private const string ValidExpenseAmount = "400,00";
    private const string ValidIncomeAmount = "1.000,00";
    private const string ExpenseDescription = "UX test expense";
    private const string IncomeDescription = "UX test income";
    private const string LoginPath = "/account/login";
    private const string LoginEmailLabel = "Correo electrónico";
    private const string LoginPasswordLabel = "Contraseña";
    private const string LoginButtonLabel = "Iniciar sesión";
    private const string GenericLoginErrorMessage = "Correo o contraseña no válidos.";
    private const string InlineCspMessageText = "Content Security Policy";
    private const string InlineScriptMessageText = "inline";
    private const string ScriptMessageText = "script";
    private const string TransactionMetricSelector = ".transactions-metric";
    private const string DashboardMetricSelector = ".app-stat-card";
    private const string DashboardMetricValueSelector = ".app-stat-card__value";
    private const string DashboardNavigationButtonLabel = "Abrir navegación";
    private const string DashboardSidebarSelector = "[data-app-sidebar]";
    private const int ExpectedDefaultCategoryCount = 8;
    private const int DesktopViewportWidth = 1280;
    private const int DesktopViewportHeight = 900;
    private const int MobileViewportWidth = 390;
    private const int MobileViewportHeight = 844;
    private static readonly string[] DefaultCategoryNames =
    [
        "Sueldo",
        "Otros ingresos",
        "Comida",
        "Transporte",
        "Vivienda",
        "Salud",
        "Ocio",
        "Gastos personales"
    ];

    [LocalSqlServerFact]
    public async Task RegisteredUser_CreatesCategoriesAndTransactions_ShowsExactMonthlyTotals()
    {
        var fixture = new PlaywrightSqlServerFixture();
        await fixture.InitializeAsync();
        Exception? testFailure = null;

        try
        {
            await RunJourneyAsync(fixture);
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
            throw new AggregateException("The finance journey and fixture cleanup both failed.", testFailure, cleanupFailure);
        }

        if (testFailure is not null)
            ExceptionDispatchInfo.Capture(testFailure).Throw();
    }

    private static async Task RunJourneyAsync(PlaywrightSqlServerFixture fixture)
    {
        await using var page = await fixture.Browser.NewPageAsync();
        var inlineCspErrors = new ConcurrentQueue<string>();
        await page.SetViewportSizeAsync(DesktopViewportWidth, DesktopViewportHeight);
        var registeredEmail = await RegisterAccountAsync(page, fixture.BaseAddress);
        page.Console += (_, message) =>
        {
            if (IsInlineScriptCspError(message))
                inlineCspErrors.Enqueue(message.Text);
        };

        await VerifyCategoryJourneyAsync(page, fixture.BaseAddress);
        Assert.Empty(inlineCspErrors);
        await VerifyResponsiveCategoriesAsync(page);
        await VerifyTransactionJourneyAsync(page, fixture.BaseAddress);
        await VerifyExistingAccountLoginErrorAsync(page, fixture.BaseAddress, registeredEmail);
    }

    private static async Task<string> RegisterAccountAsync(IPage page, Uri baseAddress)
    {
        await page.GotoAsync(BuildUrl(baseAddress, "/account/register"));
        var email = $"ux-{Guid.NewGuid():N}@example.com";
        var password = $"Ux-{Guid.NewGuid():N}-Test!9";

        await page.GetByLabel(LoginEmailLabel).FillAsync(email);
        await page.GetByLabel("Nombre para mostrar").FillAsync("UX Browser Test");
        await page.GetByLabel(LoginPasswordLabel).FillAsync(password);
        await page.GetByLabel("Confirmar contraseña").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Crear cuenta", Exact = true }).ClickAsync();
        await page.WaitForURLAsync("**/dashboard");

        Assert.True(await page.GetByRole(AriaRole.Heading, new() { Name = "Resumen", Exact = true }).IsVisibleAsync());
        return email;
    }

    private static async Task VerifyCategoryJourneyAsync(IPage page, Uri baseAddress)
    {
        await page.GotoAsync(BuildUrl(baseAddress, "/categories"));
        var categoryRows = page.Locator("[data-categories-row]");
        Assert.Equal(ExpectedDefaultCategoryCount, await categoryRows.CountAsync());

        foreach (var categoryName in DefaultCategoryNames)
            await AssertCategoryVisibilityAsync(page, categoryName, true);

        await VerifyDuplicateCategoryAsync(page);
        var incomeCategoryName = $"UX Income {Guid.NewGuid():N}";
        await SubmitCategoryAsync(page, incomeCategoryName, IncomeTypeValue);
        await AssertCategoryVisibilityAsync(page, incomeCategoryName, true);

        await page.GotoAsync(BuildUrl(baseAddress, "/categories?typeFilter=1"));
        var typeFilter = page.GetByLabel(CategoryTypeFilterLabel);
        Assert.Equal(CategoryIncomeTypeValue, await typeFilter.InputValueAsync());
        await AssertCategoryVisibilityAsync(page, "Sueldo", true);
        await AssertCategoryVisibilityAsync(page, incomeCategoryName, true);
        await AssertCategoryVisibilityAsync(page, "Comida", false);

        await typeFilter.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal(CategoryExpenseTypeValue, await typeFilter.InputValueAsync());
        await AssertCategoryVisibilityAsync(page, "Comida", true);
        await AssertCategoryVisibilityAsync(page, "Sueldo", false);
        await AssertCategoryVisibilityAsync(page, incomeCategoryName, false);
    }

    private static async Task VerifyDuplicateCategoryAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = CategoryCreateButtonLabel, Exact = true }).First.ClickAsync();
        await SubmitCategoryAsync(page, "Comida", ExpenseTypeValue);

        var modal = page.Locator(CategoryCreateModalSelector);
        Assert.True(await modal.GetByText(DuplicateCategoryMessage, new() { Exact = true }).IsVisibleAsync());
    }

    private static async Task SubmitCategoryAsync(IPage page, string name, string type)
    {
        var modal = page.Locator(CategoryCreateModalSelector);
        await modal.GetByLabel(CategoryNameLabel).FillAsync(name);
        await modal.GetByLabel(CategoryTypeLabel).SelectOptionAsync(type);
        await modal.GetByRole(AriaRole.Button, new() { Name = CategoryCreateButtonLabel, Exact = true }).ClickAsync();
    }

    private static async Task VerifyResponsiveCategoriesAsync(IPage page)
    {
        await page.SetViewportSizeAsync(DesktopViewportWidth, DesktopViewportHeight);
        Assert.True(await page.GetByRole(AriaRole.Heading, new() { Name = "Categorías", Exact = true }).IsVisibleAsync());
        Assert.True(await page.GetByRole(AriaRole.Button, new() { Name = CategoryCreateButtonLabel, Exact = true }).IsVisibleAsync());

        await page.SetViewportSizeAsync(MobileViewportWidth, MobileViewportHeight);
        Assert.True(await page.GetByRole(AriaRole.Heading, new() { Name = "Categorías", Exact = true }).IsVisibleAsync());
        await page.GetByRole(AriaRole.Button, new() { Name = CategoryCreateButtonLabel, Exact = true }).First.ClickAsync();

        var modal = page.Locator(CategoryCreateModalSelector);
        Assert.True(await modal.GetByLabel(CategoryNameLabel).IsVisibleAsync());
        Assert.True(await modal.GetByLabel(CategoryTypeLabel).IsVisibleAsync());
    }

    private static async Task VerifyTransactionJourneyAsync(IPage page, Uri baseAddress)
    {
        await page.GotoAsync(BuildUrl(baseAddress, "/transactions"));
        var year = await page.GetByLabel("Año").InputValueAsync();
        var month = await page.GetByLabel("Mes").InputValueAsync();
        await page.GotoAsync(BuildUrl(baseAddress, $"/transactions?year={year}&month={month}"));
        await page.SetViewportSizeAsync(MobileViewportWidth, MobileViewportHeight);

        var modal = await OpenTransactionModalAsync(page);
        var validDate = await GetMaximumTransactionDateAsync(modal);
        await FillTransactionAsync(modal, ExpenseTypeValue, "Comida", InvalidExpenseAmount, validDate, ExpenseDescription);
        await modal.GetByRole(AriaRole.Button, new() { Name = TransactionCreateButtonLabel, Exact = true }).ClickAsync();
        Assert.True(await modal.GetByText(InvalidAmountMessage, new() { Exact = true }).IsVisibleAsync());
        Assert.True(await modal.GetByLabel(TransactionAmountLabel).IsVisibleAsync());

        await FillTransactionAsync(modal, ExpenseTypeValue, "Comida", ValidExpenseAmount, validDate, ExpenseDescription);
        await modal.GetByRole(AriaRole.Button, new() { Name = TransactionCreateButtonLabel, Exact = true }).ClickAsync();
        Assert.True(await page.GetByText(ExpenseDescription, new() { Exact = true }).IsVisibleAsync());

        modal = await OpenTransactionModalAsync(page);
        validDate = await GetMaximumTransactionDateAsync(modal);
        await FillTransactionAsync(modal, IncomeTypeValue, "Sueldo", ValidIncomeAmount, validDate, IncomeDescription);
        await modal.GetByRole(AriaRole.Button, new() { Name = TransactionCreateButtonLabel, Exact = true }).ClickAsync();
        Assert.True(await page.GetByText(IncomeDescription, new() { Exact = true }).IsVisibleAsync());

        await AssertMetricAsync(page, "Ingresos", "1.000,00");
        await AssertMetricAsync(page, "Gastos", "400,00");
        await AssertMetricAsync(page, "Balance", "600,00");
        Assert.Equal(year, await page.GetByLabel("Año").InputValueAsync());
        Assert.Equal(month, await page.GetByLabel("Mes").InputValueAsync());
        Assert.True(await page.GetByRole(AriaRole.Heading, new() { Name = "Movimientos", Exact = true }).IsVisibleAsync());
        Assert.True(await page.GetByRole(AriaRole.Button, new() { Name = TransactionCreateButtonLabel, Exact = true }).IsVisibleAsync());

        await VerifyDashboardTotalsAndMobileNavigationAsync(page, baseAddress, year, month);
    }

    private static async Task VerifyDashboardTotalsAndMobileNavigationAsync(IPage page, Uri baseAddress, string year, string month)
    {
        await page.SetViewportSizeAsync(MobileViewportWidth, MobileViewportHeight);
        await page.GotoAsync(BuildUrl(baseAddress, $"/dashboard?year={year}&month={month}"));
        Assert.Equal(year, await page.GetByLabel("Año").InputValueAsync());
        Assert.Equal(month, await page.GetByLabel("Mes").InputValueAsync());
        await AssertDashboardMetricAsync(page, "Ingresos", "1.000,00");
        await AssertDashboardMetricAsync(page, "Gastos", "400,00");
        await AssertDashboardMetricAsync(page, "Balance", "600,00");

        var navigationButton = page.GetByRole(AriaRole.Button, new() { Name = DashboardNavigationButtonLabel, Exact = true });
        var sidebar = page.Locator(DashboardSidebarSelector);
        Assert.True(await navigationButton.IsVisibleAsync());
        Assert.Equal("false", await navigationButton.GetAttributeAsync("aria-expanded"));
        Assert.Equal("true", await sidebar.GetAttributeAsync("aria-hidden"));

        await navigationButton.ClickAsync();
        Assert.Equal("true", await navigationButton.GetAttributeAsync("aria-expanded"));
        Assert.Equal("false", await sidebar.GetAttributeAsync("aria-hidden"));

        await page.Keyboard.PressAsync("Escape");
        Assert.Equal("false", await navigationButton.GetAttributeAsync("aria-expanded"));
        Assert.Equal("true", await sidebar.GetAttributeAsync("aria-hidden"));
    }

    private static async Task VerifyExistingAccountLoginErrorAsync(IPage page, Uri baseAddress, string email)
    {
        await page.GotoAsync(BuildUrl(baseAddress, LoginPath));
        await page.GetByLabel(LoginEmailLabel).FillAsync(email);
        await page.GetByLabel(LoginPasswordLabel).FillAsync($"Wrong-{Guid.NewGuid():N}-9!");
        await page.GetByRole(AriaRole.Button, new() { Name = LoginButtonLabel, Exact = true }).ClickAsync();
        Assert.True(await page.GetByText(GenericLoginErrorMessage, new() { Exact = true }).IsVisibleAsync());
    }

    private static async Task<ILocator> OpenTransactionModalAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = TransactionCreateButtonLabel, Exact = true }).First.ClickAsync();
        return page.Locator(TransactionCreateModalSelector);
    }

    private static async Task<string> GetMaximumTransactionDateAsync(ILocator modal)
    {
        var maximumDate = await modal.GetByLabel(TransactionDateLabel).GetAttributeAsync("max");
        return maximumDate ?? throw new InvalidOperationException("The transaction date limit was not rendered.");
    }

    private static async Task FillTransactionAsync(
        ILocator modal,
        string type,
        string categoryName,
        string amount,
        string date,
        string description)
    {
        await modal.GetByLabel(TransactionTypeLabel).SelectOptionAsync(type);
        await modal.GetByLabel(TransactionCategoryLabel).SelectOptionAsync(new SelectOptionValue { Label = categoryName });
        await modal.GetByLabel(TransactionAmountLabel).FillAsync(amount);
        await modal.GetByLabel(TransactionDateLabel).FillAsync(date);
        await modal.GetByLabel(TransactionDescriptionLabel).FillAsync(description);
    }

    private static async Task AssertMetricAsync(IPage page, string label, string value)
    {
        var metric = page.Locator(TransactionMetricSelector).Filter(new LocatorFilterOptions { HasText = label });
        Assert.Equal(1, await metric.CountAsync());
        Assert.True(await metric.GetByText(value, new() { Exact = true }).IsVisibleAsync());
    }

    private static async Task AssertDashboardMetricAsync(IPage page, string label, string expectedValue)
    {
        var metric = page.Locator(DashboardMetricSelector).Filter(new LocatorFilterOptions { HasText = label });
        Assert.Equal(1, await metric.CountAsync());
        var renderedValue = await metric.Locator(DashboardMetricValueSelector).InnerTextAsync();
        var actualValue = renderedValue
            .Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        Assert.Equal(expectedValue, actualValue);
    }

    private static async Task AssertCategoryVisibilityAsync(IPage page, string categoryName, bool expectedVisibility)
    {
        var category = page.GetByText(categoryName, new() { Exact = true });
        Assert.Equal(expectedVisibility, await category.IsVisibleAsync());
    }

    private static bool IsInlineScriptCspError(IConsoleMessage message) =>
        message.Type.Equals("error", StringComparison.OrdinalIgnoreCase)
        && message.Text.Contains(InlineCspMessageText, StringComparison.OrdinalIgnoreCase)
        && message.Text.Contains(InlineScriptMessageText, StringComparison.OrdinalIgnoreCase)
        && message.Text.Contains(ScriptMessageText, StringComparison.OrdinalIgnoreCase);

    private static string BuildUrl(Uri baseAddress, string path) => new Uri(baseAddress, path).ToString();
}
