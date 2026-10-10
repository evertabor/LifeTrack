using LifeTrack.Domain.Common;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Domain.Tests.Finance;

public class CategoryTests
{
    [Fact]
    public void Create_WithValidName_ReturnsCategory()
    {
        var userId = Guid.NewGuid();
        var result = Category.Create("Comida", userId);
        Assert.Equal("Comida", result.Name);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public void Create_WithWhitespaceAroundName_TrimsAndStoresTrimmed()
    {
        var userId = Guid.NewGuid();
        var result = Category.Create("  Comida  ", userId);
        Assert.Equal("Comida", result.Name);
    }

    [Fact]
    public void Create_WithEmptyString_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();
        var ex = Assert.Throws<DomainException>(() => Category.Create("", userId));
        Assert.Contains("obligatorio", ex.Message);
    }

    [Fact]
    public void Create_WithWhitespaceOnly_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();
        var ex = Assert.Throws<DomainException>(() => Category.Create("   ", userId));
        Assert.Contains("obligatorio", ex.Message);
    }

    [Fact]
    public void Create_WithExactly50Characters_Succeeds()
    {
        var userId = Guid.NewGuid();
        var name = new string('a', 50);
        var result = Category.Create(name, userId);
        Assert.Equal(50, result.Name.Length);
    }

    [Fact]
    public void Create_With51Characters_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();
        var name = new string('a', 51);
        var ex = Assert.Throws<DomainException>(() => Category.Create(name, userId));
        Assert.Contains("50", ex.Message);
    }
}
