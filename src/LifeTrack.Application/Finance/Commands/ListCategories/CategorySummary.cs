using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Finance.Commands.ListCategories;

public sealed record CategorySummary(Guid Id, string Name, bool IsIncome)
{
    public string TypeLabel => IsIncome ? "Ingreso" : "Gasto";

    public TransactionType Type => IsIncome ? TransactionType.Income : TransactionType.Expense;
}
