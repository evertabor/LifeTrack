using LifeTrack.Application.Finance;
using LifeTrack.Application.Identity;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Identity;

public class RegisterAccountHandlerTests
{
    [Fact]
    public async Task RegisterAccountHandler_ValidInput_ReturnsSuccess()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(true, null),
            new LoginOperationResult(false, "Irrelevant"));
        var handler = new RegisterAccountHandler(fakeService, new Finance.Fakes.FakeCategoryRepository(), new FakeUnitOfWork());
        var command = new RegisterAccountCommand("test@example.com", "VeryStrongPassword123!", "Test User", null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task RegisterAccountHandler_ValidInput_ForwardsProfileFields()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(true, null),
            new LoginOperationResult(false, "Irrelevant"));
        var handler = new RegisterAccountHandler(fakeService, new Finance.Fakes.FakeCategoryRepository(), new FakeUnitOfWork());
        var command = new RegisterAccountCommand("test@example.com", "VeryStrongPassword123!", "Test User", "600123456");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(("test@example.com", "VeryStrongPassword123!", "Test User", "600123456"), fakeService.LastRegister);
    }

    [Fact]
    public async Task RegisterAccountHandler_DuplicateEmail_ReturnsDuplicateError()
    {
        var duplicateErrors = new[] { "Email 'test@example.com' is already taken." };
        var fakeService = new FakeAccountService(
            new AccountOperationResult(false, duplicateErrors),
            new LoginOperationResult(false, "Irrelevant"));
        var handler = new RegisterAccountHandler(fakeService, new Finance.Fakes.FakeCategoryRepository(), new FakeUnitOfWork());
        var command = new RegisterAccountCommand("test@example.com", "VeryStrongPassword123!", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors!, e => e.Contains("Ya existe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisterAccountHandler_WeakPassword_ReturnsIdentityErrors()
    {
        var passwordErrors = new[] { "Password must be at least 12 characters." };
        var fakeService = new FakeAccountService(
            new AccountOperationResult(false, passwordErrors),
            new LoginOperationResult(false, "Irrelevant"));
        var handler = new RegisterAccountHandler(fakeService, new Finance.Fakes.FakeCategoryRepository(), new FakeUnitOfWork());
        var command = new RegisterAccountCommand("test@example.com", "weak", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors!, e => e.Contains("12", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisterAccountHandler_ValidInput_SeedsDefaultCategories()
    {
        var userId = Guid.NewGuid();
        var fakeService = new FakeAccountService(
            new AccountOperationResult(true, null, userId),
            new LoginOperationResult(false, "Irrelevant"));
        var repository = new Finance.Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegisterAccountHandler(fakeService, repository, unitOfWork);
        var command = new RegisterAccountCommand("test@example.com", "VeryStrongPassword123!", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        var categories = await repository.ListAsync(userId, CancellationToken.None);
        Assert.Equal(DefaultCategories.Names, categories.Select(c => c.Name).ToList());
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RegisterAccountHandler_FailedRegistration_SeedsNothing()
    {
        var fakeService = new FakeAccountService(
            new AccountOperationResult(false, new[] { "Password must be at least 12 characters." }),
            new LoginOperationResult(false, "Irrelevant"));
        var repository = new Finance.Fakes.FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegisterAccountHandler(fakeService, repository, unitOfWork);
        var command = new RegisterAccountCommand("test@example.com", "weak", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RegisterAccountHandler_ExistingDefaultName_SkipsDuplicate()
    {
        var userId = Guid.NewGuid();
        var fakeService = new FakeAccountService(
            new AccountOperationResult(true, null, userId),
            new LoginOperationResult(false, "Irrelevant"));
        var repository = new Finance.Fakes.FakeCategoryRepository();
        await repository.AddAsync(Category.Create("Comida", userId, TransactionType.Expense), CancellationToken.None);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegisterAccountHandler(fakeService, repository, unitOfWork);
        var command = new RegisterAccountCommand("test@example.com", "VeryStrongPassword123!", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        var categories = await repository.ListAsync(userId, CancellationToken.None);
        Assert.Equal(DefaultCategories.Names.OrderBy(n => n), categories.Select(c => c.Name).OrderBy(n => n));
    }
}
