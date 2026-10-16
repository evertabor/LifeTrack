using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.ImportTransactions;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Application.Tests.Finance.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class TransactionCsvParserTests
{
    [Fact]
    public void ParseRows_WithSemicolons_SplitsFields()
    {
        var rows = TransactionCsvParser.ParseRows("06/10/2026;Comida;Gasto;25,50;Compra");

        var row = Assert.Single(rows);
        Assert.Equal(["06/10/2026", "Comida", "Gasto", "25,50", "Compra"], row);
    }

    [Fact]
    public void ParseRows_WithCommas_SplitsFields()
    {
        var rows = TransactionCsvParser.ParseRows("2026-10-06,Comida,Gasto,25.50,Compra");

        var row = Assert.Single(rows);
        Assert.Equal(5, row.Length);
    }

    [Fact]
    public void ParseRows_WithQuotedSeparator_KeepsField()
    {
        var rows = TransactionCsvParser.ParseRows("06/10/2026;Comida;Gasto;25,50;\"Compra; grande\"");

        var row = Assert.Single(rows);
        Assert.Equal("Compra; grande", row[4]);
    }

    [Fact]
    public void ParseRows_WithBlankLines_SkipsThem()
    {
        var rows = TransactionCsvParser.ParseRows("06/10/2026;Comida;Gasto;10;Compra\n\n07/10/2026;Comida;Gasto;5;Otra");

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void ParseRows_WithEmptyContent_ReturnsEmpty()
    {
        Assert.Empty(TransactionCsvParser.ParseRows(string.Empty));
    }
}

public class PreviewTransactionsHandlerTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static async Task<(PreviewTransactionsHandler Handler, Guid FoodId)> ArrangeAsync()
    {
        var userId = Guid.NewGuid();
        var categories = new FakeCategoryRepository();
        var food = Category.Create("Comida", userId, TransactionType.Expense);
        await categories.AddAsync(food, CancellationToken.None);
        var handler = new PreviewTransactionsHandler(categories, new FakeCurrentUser(userId), new FixedTimeProvider(Today));
        return (handler, food.Id);
    }

    [Fact]
    public async Task Handle_MixedRows_SplitsValidAndInvalid()
    {
        var arranged = await ArrangeAsync();
        var content = "06/10/2026;Comida;Gasto;25,50;Compra\n06/10/2026;Comida;Gasto;0;Sin importe";

        var result = await arranged.Handler.Handle(new PreviewTransactionsQuery(content), CancellationToken.None);

        Assert.Equal(2, result.TotalRows);
        Assert.Single(result.ValidRows);
        var invalid = Assert.Single(result.InvalidRows);
        Assert.Equal(2, invalid.LineNumber);
        Assert.False(string.IsNullOrEmpty(invalid.Error));
    }

    [Fact]
    public async Task Handle_ForeignCategory_MarksRowInvalid()
    {
        var arranged = await ArrangeAsync();
        var content = "06/10/2026;Restaurantes;Gasto;10;Compra";

        var result = await arranged.Handler.Handle(new PreviewTransactionsQuery(content), CancellationToken.None);

        Assert.Empty(result.ValidRows);
        Assert.Single(result.InvalidRows);
    }

    [Fact]
    public async Task Handle_MismatchedType_MarksRowInvalid()
    {
        var arranged = await ArrangeAsync();
        var content = "06/10/2026;Comida;Ingreso;1000;Sueldo";

        var result = await arranged.Handler.Handle(new PreviewTransactionsQuery(content), CancellationToken.None);

        Assert.Empty(result.ValidRows);
        var invalid = Assert.Single(result.InvalidRows);
        Assert.Contains("Comida", invalid.Error);
    }

    [Fact]
    public async Task Handle_EmptyContent_ThrowsBusinessRule()
    {
        var arranged = await ArrangeAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(new PreviewTransactionsQuery("   "), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TooManyRows_ThrowsBusinessRule()
    {
        var arranged = await ArrangeAsync();
        var content = string.Join("\n", Enumerable.Repeat("06/10/2026;Comida;Gasto;1;Compra", TransactionCsvParser.MaxRows + 1));

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(new PreviewTransactionsQuery(content), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var handler = new PreviewTransactionsHandler(
            new FakeCategoryRepository(), new FakeCurrentUser(null), new FixedTimeProvider(Today));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new PreviewTransactionsQuery("06/10/2026;Comida;Gasto;1;Compra"), CancellationToken.None));
        Assert.Contains("usuario autenticado", ex.Message);
    }
}

public class ConfirmTransactionImportHandlerTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static async Task<(ConfirmTransactionImportHandler Handler, FakeTransactionRepository Transactions, FakeUnitOfWork UnitOfWork, Guid UserId)> ArrangeAsync()
    {
        var userId = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        await categories.AddAsync(Category.Create("Comida", userId, TransactionType.Expense), CancellationToken.None);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ConfirmTransactionImportHandler(transactions, categories, unitOfWork, new FakeCurrentUser(userId), new FixedTimeProvider(Today));
        return (handler, transactions, unitOfWork, userId);
    }

    [Fact]
    public async Task Handle_MixedContent_SavesOnlyValidAndReturnsCount()
    {
        var arranged = await ArrangeAsync();
        var content = "06/10/2026;Comida;Gasto;25,50;Compra\n06/10/2026;Comida;Gasto;0;Sin importe";

        var confirmation = await arranged.Handler.Handle(new ConfirmTransactionImportCommand(content), CancellationToken.None);

        Assert.Equal(1, confirmation.Imported);
        Assert.Equal(1, confirmation.Skipped);
        Assert.Equal(1, arranged.UnitOfWork.SaveCount);
        var stored = await arranged.Transactions.ListByPeriodAsync(arranged.UserId, new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 1), CancellationToken.None);
        Assert.Single(stored);
    }

    [Fact]
    public async Task Handle_OversizedContent_ThrowsBusinessRuleAndSavesNothing()
    {
        var arranged = await ArrangeAsync();
        var content = new string('a', TransactionCsvParser.MaxContentBytes + 1);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(new ConfirmTransactionImportCommand(content), CancellationToken.None));
        Assert.Contains("1 MB", ex.Message);
        Assert.Equal(0, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_NoValidRows_ThrowsBusinessRuleAndSavesNothing()
    {
        var arranged = await ArrangeAsync();
        var content = "06/10/2026;Restaurantes;Gasto;10;Compra";

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => arranged.Handler.Handle(new ConfirmTransactionImportCommand(content), CancellationToken.None));
        Assert.Equal(0, arranged.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_CategoryOfAnotherUser_SavesNothing()
    {
        var owner = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        await categories.AddAsync(Category.Create("Comida", Guid.NewGuid(), TransactionType.Expense), CancellationToken.None);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ConfirmTransactionImportHandler(transactions, categories, unitOfWork, new FakeCurrentUser(owner), new FixedTimeProvider(Today));
        var content = "06/10/2026;Comida;Gasto;10;Compra";

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new ConfirmTransactionImportCommand(content), CancellationToken.None));
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_CategoryRemovedAfterPreview_SkipsRowAndImportsTheRest()
    {
        var userId = Guid.NewGuid();
        var transactions = new FakeTransactionRepository();
        var categories = new FakeCategoryRepository();
        await categories.AddAsync(Category.Create("Comida", userId, TransactionType.Expense), CancellationToken.None);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ConfirmTransactionImportHandler(transactions, categories, unitOfWork, new FakeCurrentUser(userId), new FixedTimeProvider(Today));
        categories.Reset();
        await categories.AddAsync(Category.Create("Ocio", userId, TransactionType.Expense), CancellationToken.None);
        var content = "06/10/2026;Comida;Gasto;10;Compra\n06/10/2026;Ocio;Gasto;5;Cine";

        var confirmation = await handler.Handle(new ConfirmTransactionImportCommand(content), CancellationToken.None);

        Assert.Equal(1, confirmation.Imported);
        Assert.Equal(1, confirmation.Skipped);
        Assert.Equal(1, unitOfWork.SaveCount);
    }
}
