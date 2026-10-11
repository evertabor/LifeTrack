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

    [Fact]
    public void Validate_MissingType_HasTypeError()
    {
        var errors = Validate(ValidInput() with { Type = null });

        Assert.Contains(errors, r => r.MemberNames.Contains("Type"));
    }
}

public class EditTransactionInputModelValidationTests
{
    private static EditTransactionInputModel ValidInput() => new()
    {
        CategoryId = Guid.NewGuid(),
        Type = true,
        Amount = 100m,
        Date = new DateOnly(2026, 10, 9),
        Description = "Sueldo"
    };

    private static IReadOnlyList<ValidationResult> Validate(EditTransactionInputModel input)
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
    public void Validate_MissingType_HasTypeError()
    {
        var errors = Validate(ValidInput() with { Type = null });

        Assert.Contains(errors, r => r.MemberNames.Contains("Type"));
    }
}

public class SetBudgetInputModelValidationTests
{
    private static SetBudgetInputModel ValidInput() => new()
    {
        CategoryId = Guid.NewGuid(),
        Year = 2026,
        Month = 10,
        Limit = 100m
    };

    private static IReadOnlyList<ValidationResult> Validate(SetBudgetInputModel input)
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
    public void Validate_ZeroLimit_HasLimitError()
    {
        var errors = Validate(ValidInput() with { Limit = 0m });

        Assert.Contains(errors, r => r.MemberNames.Contains("Limit"));
    }

    [Fact]
    public void Validate_MissingCategory_HasCategoryError()
    {
        var errors = Validate(ValidInput() with { CategoryId = null });

        Assert.Contains(errors, r => r.MemberNames.Contains("CategoryId"));
    }
}
