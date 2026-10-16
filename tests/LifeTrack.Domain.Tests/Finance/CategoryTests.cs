using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Domain.Tests.Finance;

public class CategoryTests
{
    [Fact]
    public void Create_WithValidName_ReturnsCategory()
    {
        var userId = Guid.NewGuid();
        var result = Category.Create("Comida", userId, TransactionType.Expense);
        Assert.Equal("Comida", result.Name);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(TransactionType.Expense, result.Type);
    }

    [Fact]
    public void Create_WithIncomeType_StoresIncomeType()
    {
        var result = Category.Create("Sueldo", Guid.NewGuid(), TransactionType.Income);
        Assert.Equal(TransactionType.Income, result.Type);
    }

    [Fact]
    public void Create_WithWhitespaceAroundName_TrimsAndStoresTrimmed()
    {
        var userId = Guid.NewGuid();
        var result = Category.Create("  Comida  ", userId, TransactionType.Expense);
        Assert.Equal("Comida", result.Name);
    }

    [Fact]
    public void Create_WithNullName_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();
        var ex = Assert.Throws<DomainException>(() => Category.Create(null, userId, TransactionType.Expense));
        Assert.Contains("obligatorio", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyString_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();
        var ex = Assert.Throws<DomainException>(() => Category.Create("", userId, TransactionType.Expense));
        Assert.Contains("obligatorio", ex.Message);
    }

    [Fact]
    public void Create_WithWhitespaceOnly_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();
        var ex = Assert.Throws<DomainException>(() => Category.Create("   ", userId, TransactionType.Expense));
        Assert.Contains("obligatorio", ex.Message);
    }

    [Fact]
    public void Create_WithExactly50Characters_Succeeds()
    {
        var userId = Guid.NewGuid();
        var name = new string('a', 50);
        var result = Category.Create(name, userId, TransactionType.Expense);
        Assert.Equal(50, result.Name.Length);
    }

    [Fact]
    public void Create_With51Characters_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();
        var name = new string('a', 51);
        var ex = Assert.Throws<DomainException>(() => Category.Create(name, userId, TransactionType.Expense));
        Assert.Contains("50", ex.Message);
    }

    [Fact]
    public void Create_WithUndefinedType_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => Category.Create("Comida", Guid.NewGuid(), (TransactionType)99));
        Assert.Contains("tipo", ex.Message);
    }

    [Fact]
    public void EditName_WithValidName_UpdatesName()
    {
        var category = Category.Create("Comida", Guid.NewGuid(), TransactionType.Expense);
        category.EditName("Transporte");
        Assert.Equal("Transporte", category.Name);
    }

    [Fact]
    public void EditName_WithWhitespaceAroundName_TrimsAndStoresTrimmed()
    {
        var category = Category.Create("Comida", Guid.NewGuid(), TransactionType.Expense);
        category.EditName("  Transporte  ");
        Assert.Equal("Transporte", category.Name);
    }

    [Fact]
    public void EditName_WithEmptyString_ThrowsDomainException()
    {
        var category = Category.Create("Comida", Guid.NewGuid(), TransactionType.Expense);
        var ex = Assert.Throws<DomainException>(() => category.EditName(""));
        Assert.Contains("obligatorio", ex.Message);
    }

    [Fact]
    public void EditName_With51Characters_ThrowsDomainException()
    {
        var category = Category.Create("Comida", Guid.NewGuid(), TransactionType.Expense);
        var name = new string('a', 51);
        var ex = Assert.Throws<DomainException>(() => category.EditName(name));
        Assert.Contains("50", ex.Message);
    }
}
