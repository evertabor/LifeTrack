using System.ComponentModel.DataAnnotations;
using LifeTrack.Web.ViewModels.Account;

namespace LifeTrack.Integration.Tests;

public class RegisterInputModelValidationTests
{
    private static RegisterInputModel ValidInput() => new()
    {
        Email = "test@example.com",
        DisplayName = "Test User",
        PhoneNumber = "600123456",
        Password = "VeryStrongPassword123!",
        ConfirmPassword = "VeryStrongPassword123!"
    };

    private static IReadOnlyList<ValidationResult> Validate(RegisterInputModel input)
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
    public void Validate_OptionalFieldsEmpty_HasNoErrors()
    {
        Assert.Empty(Validate(ValidInput() with { DisplayName = null, PhoneNumber = null }));
    }

    [Fact]
    public void Validate_PasswordMismatch_HasConfirmError()
    {
        var errors = Validate(ValidInput() with { ConfirmPassword = "Different123!" });

        Assert.Contains(errors, r => r.MemberNames.Contains("ConfirmPassword"));
    }

    [Fact]
    public void Validate_LongDisplayName_HasDisplayNameError()
    {
        var errors = Validate(ValidInput() with { DisplayName = new string('a', RegisterInputModel.DisplayNameMaxLength + 1) });

        Assert.Contains(errors, r => r.MemberNames.Contains("DisplayName"));
    }

    [Fact]
    public void Validate_LongPhoneNumber_HasPhoneNumberError()
    {
        var errors = Validate(ValidInput() with { PhoneNumber = new string('1', RegisterInputModel.PhoneNumberMaxLength + 1) });

        Assert.Contains(errors, r => r.MemberNames.Contains("PhoneNumber"));
    }

    [Fact]
    public void RegisterViewModel_From_CarriesInputAndError()
    {
        var viewModel = RegisterViewModel.From(ValidInput(), "Fallo el registro.");

        Assert.Equal("test@example.com", viewModel.Email);
        Assert.Equal("Fallo el registro.", viewModel.ErrorMessage);
        Assert.True(viewModel.HasError);
        Assert.False(new RegisterViewModel().HasError);
        Assert.Contains("12", viewModel.PasswordHelpText);
    }
}

public class EditProfileInputModelValidationTests
{
    private static EditProfileInputModel ValidInput() => new()
    {
        DisplayName = "Test User",
        PhoneNumber = "600123456"
    };

    private static IReadOnlyList<ValidationResult> Validate(EditProfileInputModel input)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, true);
        return results;
    }

    [Fact]
    public void Validate_EmptyInput_HasNoErrors()
    {
        Assert.Empty(Validate(new EditProfileInputModel()));
    }

    [Fact]
    public void Validate_ValidInput_HasNoErrors()
    {
        Assert.Empty(Validate(ValidInput()));
    }

    [Fact]
    public void Validate_LongDisplayName_HasDisplayNameError()
    {
        var errors = Validate(ValidInput() with { DisplayName = new string('a', EditProfileInputModel.DisplayNameMaxLength + 1) });

        Assert.Contains(errors, r => r.MemberNames.Contains("DisplayName"));
    }

    [Fact]
    public void Validate_LongPhoneNumber_HasPhoneNumberError()
    {
        var errors = Validate(ValidInput() with { PhoneNumber = new string('1', EditProfileInputModel.PhoneNumberMaxLength + 1) });

        Assert.Contains(errors, r => r.MemberNames.Contains("PhoneNumber"));
    }
}
