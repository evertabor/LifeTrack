using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Transactions;

public sealed class AmountRangeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var amount = (decimal)value;

        if (amount < CreateTransactionInputModel.MinAmount || amount > CreateTransactionInputModel.MaxAmount)
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}
