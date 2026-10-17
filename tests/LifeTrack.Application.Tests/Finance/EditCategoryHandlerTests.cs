using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.EditCategory;
using LifeTrack.Application.Tests.Fakes;
using LifeTrack.Application.Tests.Finance.Fakes;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance;

public class EditCategoryHandlerTests
{
    private static readonly DateOnly MovementDate = new(2026, 1, 15);
    private static readonly DateOnly Today = new(2026, 3, 1);

    private readonly FakeCategoryRepository _repository = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Handle_ValidName_UpdatesName()
    {
        var userId = Guid.NewGuid();
        var category = await GivenCategoryAsync("Comida", userId, TransactionType.Expense);
        var handler = GivenHandler(userId);

        await handler.Handle(new EditCategoryCommand(category.Id, "Transporte", false), CancellationToken.None);

        var categories = await _repository.ListAsync(userId, CancellationToken.None);
        Assert.Equal("Transporte", Assert.Single(categories).Name);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_SameNameAndType_Succeeds()
    {
        var userId = Guid.NewGuid();
        var category = await GivenCategoryAsync("Comida", userId, TransactionType.Expense);
        var handler = GivenHandler(userId);

        await handler.Handle(new EditCategoryCommand(category.Id, "Comida", false), CancellationToken.None);

        var categories = await _repository.ListAsync(userId, CancellationToken.None);
        Assert.Equal(TransactionType.Expense, Assert.Single(categories).Type);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_ChangeTypeWithoutMovements_ChangesType()
    {
        var userId = Guid.NewGuid();
        var category = await GivenCategoryAsync("Comida", userId, TransactionType.Expense);
        var handler = GivenHandler(userId);

        await handler.Handle(new EditCategoryCommand(category.Id, "Comida", true), CancellationToken.None);

        var categories = await _repository.ListAsync(userId, CancellationToken.None);
        Assert.Equal(TransactionType.Income, Assert.Single(categories).Type);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_ChangeTypeWithMovements_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        var category = await GivenCategoryAsync("Comida", userId, TransactionType.Expense);
        await _transactions.AddAsync(
            Transaction.Create(
                userId,
                new TransactionData(category.Id, TransactionType.Expense, 25m, MovementDate, null),
                Today,
                category),
            CancellationToken.None);
        var handler = GivenHandler(userId);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new EditCategoryCommand(category.Id, "Comida", true), CancellationToken.None));

        Assert.Contains("movimientos", ex.Message);
        Assert.Equal(0, _unitOfWork.SaveCount);
        var categories = await _repository.ListAsync(userId, CancellationToken.None);
        Assert.Equal(TransactionType.Expense, Assert.Single(categories).Type);
    }

    [Fact]
    public async Task Handle_ChangeTypeWithBudgets_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        var category = await GivenCategoryAsync("Comida", userId, TransactionType.Expense);
        await _budgets.AddAsync(
            Budget.Create(userId, category.Id, new BudgetPeriod(2026, 1), 300m, category),
            CancellationToken.None);
        var handler = GivenHandler(userId);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new EditCategoryCommand(category.Id, "Comida", true), CancellationToken.None));

        Assert.Contains("presupuestos", ex.Message);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        await GivenCategoryAsync("Comida", userId, TransactionType.Expense);
        var category = await GivenCategoryAsync("Transporte", userId, TransactionType.Expense);
        var handler = GivenHandler(userId);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new EditCategoryCommand(category.Id, "Comida", false), CancellationToken.None));

        Assert.Equal("Ya existe una categoría con ese nombre.", ex.Message);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_EmptyName_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        var category = await GivenCategoryAsync("Comida", userId, TransactionType.Expense);
        var handler = GivenHandler(userId);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new EditCategoryCommand(category.Id, "", false), CancellationToken.None));

        Assert.Contains("obligatorio", ex.Message);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_NameTooLong_ThrowsBusinessRuleException()
    {
        var userId = Guid.NewGuid();
        var category = await GivenCategoryAsync("Comida", userId, TransactionType.Expense);
        var handler = GivenHandler(userId);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new EditCategoryCommand(category.Id, new string('a', 51), false), CancellationToken.None));

        Assert.Contains("50", ex.Message);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_CategoryOfAnotherUser_ThrowsResourceNotFound()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var category = await GivenCategoryAsync("Comida", userA, TransactionType.Expense);
        var handler = GivenHandler(userB);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(new EditCategoryCommand(category.Id, "Transporte", true), CancellationToken.None));

        Assert.Equal(0, _unitOfWork.SaveCount);
        var categories = await _repository.ListAsync(userA, CancellationToken.None);
        Assert.Equal(TransactionType.Expense, Assert.Single(categories).Type);
    }

    [Fact]
    public async Task Handle_MissingCategory_ThrowsResourceNotFound()
    {
        var userId = Guid.NewGuid();
        var handler = GivenHandler(userId);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.Handle(new EditCategoryCommand(Guid.NewGuid(), "Transporte", false), CancellationToken.None));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ThrowsBusinessRuleException()
    {
        var handler = GivenHandler(null);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new EditCategoryCommand(Guid.NewGuid(), "Transporte", false), CancellationToken.None));

        Assert.Contains("usuario autenticado", ex.Message);
    }

    private EditCategoryHandler GivenHandler(Guid? userId) =>
        new(_repository, _transactions, _budgets, _unitOfWork, new FakeCurrentUser(userId));

    private async Task<Category> GivenCategoryAsync(string name, Guid userId, TransactionType type)
    {
        var category = Category.Create(name, userId, type);
        await _repository.AddAsync(category, CancellationToken.None);
        return category;
    }
}
