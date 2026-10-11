using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Domain.Tests.Finance;

public class BudgetTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ReturnsBudget()
    {
        var result = Budget.Create(UserId, CategoryId, new BudgetPeriod(2026, 10), 100m);

        Assert.Equal(100m, result.Limit);
        Assert.Equal(2026, result.Year);
        Assert.Equal(10, result.Month);
    }

    [Fact]
    public void Create_WithZeroLimit_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Budget.Create(UserId, CategoryId, new BudgetPeriod(2026, 10), 0m));
    }

    [Fact]
    public void Create_WithThreeDecimals_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Budget.Create(UserId, CategoryId, new BudgetPeriod(2026, 10), 10.123m));
    }

    [Fact]
    public void Create_WithMonth13_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Budget.Create(UserId, CategoryId, new BudgetPeriod(2026, 13), 100m));
    }

    [Fact]
    public void Create_WithYear1999_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Budget.Create(UserId, CategoryId, new BudgetPeriod(1999, 10), 100m));
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Budget.Create(Guid.Empty, CategoryId, new BudgetPeriod(2026, 10), 100m));
    }

    [Fact]
    public void ChangeLimit_WithValidLimit_UpdatesLimit()
    {
        var budget = Budget.Create(UserId, CategoryId, new BudgetPeriod(2026, 10), 100m);

        budget.ChangeLimit(150m);

        Assert.Equal(150m, budget.Limit);
    }

    [Fact]
    public void ChangeLimit_WithZeroLimit_ThrowsDomainException()
    {
        var budget = Budget.Create(UserId, CategoryId, new BudgetPeriod(2026, 10), 100m);

        Assert.Throws<DomainException>(() => budget.ChangeLimit(0m));
    }
}
