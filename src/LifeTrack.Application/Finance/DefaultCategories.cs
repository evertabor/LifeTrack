using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Finance;

public sealed record DefaultCategory(string Name, TransactionType Type);

public static class DefaultCategories
{
    public static IReadOnlyList<DefaultCategory> All { get; } =
    [
        new("Sueldo", TransactionType.Income),
        new("Otros ingresos", TransactionType.Income),
        new("Comida", TransactionType.Expense),
        new("Transporte", TransactionType.Expense),
        new("Vivienda", TransactionType.Expense),
        new("Salud", TransactionType.Expense),
        new("Ocio", TransactionType.Expense),
        new("Gastos personales", TransactionType.Expense)
    ];

    public static IReadOnlyList<string> Names => All.Select(c => c.Name).ToList();
}
