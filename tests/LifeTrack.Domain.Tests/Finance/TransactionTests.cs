using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Domain.Tests.Finance;

public class TransactionTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static Transaction CreateValid(
        decimal amount = 25.50m,
        DateOnly? date = null,
        string? description = null,
        TransactionType type = TransactionType.Expense)
    {
        return Transaction.Create(UserId, CategoryId, type, amount, date ?? Today, description, Today);
    }

    [Fact]
    public void Create_WithValidData_ReturnsTransaction()
    {
        var result = CreateValid();

        Assert.Equal(UserId, result.UserId);
        Assert.Equal(CategoryId, result.CategoryId);
        Assert.Equal(TransactionType.Expense, result.Type);
        Assert.Equal(25.50m, result.Amount);
        Assert.Equal(Today, result.Date);
        Assert.Equal(string.Empty, result.Description);
    }

    [Fact]
    public void Create_WithZeroAmount_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CreateValid(amount: 0m));
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CreateValid(amount: -10m));
    }

    [Fact]
    public void Create_WithThreeDecimals_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CreateValid(amount: 10.123m));
    }

    [Fact]
    public void Create_WithAmountAboveMaximum_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CreateValid(amount: 1_000_000_001m));
    }

    [Fact]
    public void Create_WithTomorrowDate_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CreateValid(date: Today.AddDays(1)));
    }

    [Fact]
    public void Create_WithTodayDate_Succeeds()
    {
        var result = CreateValid(date: Today);

        Assert.Equal(Today, result.Date);
    }

    [Fact]
    public void Create_WithDescription_TrimsAndStoresTrimmed()
    {
        var result = CreateValid(description: "  Compra  ");

        Assert.Equal("Compra", result.Description);
    }

    [Fact]
    public void Create_With201CharacterDescription_ThrowsDomainException()
    {
        var description = new string('a', 201);

        Assert.Throws<DomainException>(() => CreateValid(description: description));
    }

    [Fact]
    public void Create_WithUndefinedType_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CreateValid(type: (TransactionType)99));
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Transaction.Create(Guid.Empty, CategoryId, TransactionType.Expense, 10m, Today, null, Today));
    }

    [Fact]
    public void Create_WithEmptyCategoryId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Transaction.Create(UserId, Guid.Empty, TransactionType.Expense, 10m, Today, null, Today));
    }
}
