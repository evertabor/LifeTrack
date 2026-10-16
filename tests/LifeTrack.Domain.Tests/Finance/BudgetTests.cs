using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Domain.Tests.Finance;

public class BudgetTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Category ExpenseCategory = Category.Create("Comida", UserId, TransactionType.Expense);
    private static readonly Category IncomeCategory = Category.Create("Sueldo", UserId, TransactionType.Income);

    private static Budget CreateValid(decimal limit = 100m) =>
        Budget.Create(UserId, ExpenseCategory.Id, new BudgetPeriod(2026, 10), limit, ExpenseCategory);

    [Fact]
    public void Create_WithValidData_ReturnsBudget()
    {
        var result = CreateValid();

        Assert.Equal(100m, result.Limit);
        Assert.Equal(2026, result.Year);
        Assert.Equal(10, result.Month);
    }

    [Fact]
    public void Create_WithZeroLimit_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CreateValid(limit: 0m));
    }

    [Fact]
    public void Create_WithThreeDecimals_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CreateValid(limit: 10.123m));
    }

    [Fact]
    public void Create_WithMonth13_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Budget.Create(UserId, ExpenseCategory.Id, new BudgetPeriod(2026, 13), 100m, ExpenseCategory));
    }

    [Fact]
    public void Create_WithYear1999_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Budget.Create(UserId, ExpenseCategory.Id, new BudgetPeriod(1999, 10), 100m, ExpenseCategory));
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Budget.Create(Guid.Empty, ExpenseCategory.Id, new BudgetPeriod(2026, 10), 100m, ExpenseCategory));
    }

    [Fact]
    public void Create_WithIncomeCategory_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => Budget.Create(UserId, IncomeCategory.Id, new BudgetPeriod(2026, 10), 100m, IncomeCategory));
        Assert.Contains("gasto", ex.Message);
    }

    [Fact]
    public void Create_WithDifferentCategoryEntity_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Budget.Create(UserId, ExpenseCategory.Id, new BudgetPeriod(2026, 10), 100m, IncomeCategory));
    }

    [Fact]
    public void ChangeLimit_WithValidLimit_UpdatesLimit()
    {
        var budget = CreateValid();

        budget.ChangeLimit(150m);

        Assert.Equal(150m, budget.Limit);
    }

    [Fact]
    public void ChangeLimit_WithZeroLimit_ThrowsDomainException()
    {
        var budget = CreateValid();

        Assert.Throws<DomainException>(() => budget.ChangeLimit(0m));
    }
}
