using System.ComponentModel.DataAnnotations;
using LifeTrack.Web.ViewModels.Transactions;

namespace LifeTrack.Integration.Tests;

public class CreateTransactionInputModelValidationTests
{
    private static CreateTransactionInputModel ValidInput() => new()
    {
        CategoryId = Guid.NewGuid(),
        Type = false,
        Amount = 25.50m,
        Date = new DateOnly(2026, 10, 10),
        Description = "Compra"
    };

    private static IReadOnlyList<ValidationResult> Validate(CreateTransactionInputModel input)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, true);
        return results;
    }

    [Fact]
    public void Validate_ValidInput_HasNoErrors()
    {
        Assert.Empty(Validate(ValidInput()));
    }

    [Fact]
    public void Validate_ZeroAmount_HasAmountError()
    {
        var errors = Validate(ValidInput() with { Amount = 0m });

        Assert.Contains(errors, r => r.MemberNames.Contains("Amount"));
    }

    [Fact]
    public void Validate_MissingCategory_HasCategoryError()
    {
        var errors = Validate(ValidInput() with { CategoryId = null });

        Assert.Contains(errors, r => r.MemberNames.Contains("CategoryId"));
    }

    [Fact]
    public void Validate_MissingDate_HasDateError()
    {
        var errors = Validate(ValidInput() with { Date = null });

        Assert.Contains(errors, r => r.MemberNames.Contains("Date"));
    }
}
