# Parte B — Domain  ✅

## Guard (validaciones reutilizables, internas al dominio)

`src/LifeTrack.Domain/Common/Guard.cs`

```csharp
namespace LifeTrack.Domain.Common;

internal static class Guard
{
    public static string NotBlank(string? value, int maxLength, string fieldName)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainException($"{fieldName} es obligatorio.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{fieldName} no puede superar {maxLength} caracteres.");
        }

        return trimmed;
    }

    public static Guid NotEmpty(Guid value, string fieldName) =>
        value == Guid.Empty ? throw new DomainException($"{fieldName} es obligatorio.") : value;
}
```

## Transaction (fábrica con invariantes; la fecha 'hoy' llega como parámetro)

Todas las invariantes se validan en la fábrica `Create(...)`, y ninguna se deduce del nombre: el
método devuelve la entidad ya válida o lanza `DomainException`. Los mensajes van en español y son
seguros de mostrar al usuario, así que `Application` los puede reenviar tal cual a `BusinessRuleException`.

El parámetro `today` es **la fecha actual del usuario en su zona horaria**, y se inyecta en lugar de
leerla del reloj: así la regla de "no se admiten fechas futuras" es determinista y se puede probar
fijando una fecha concreta, sin depender del día en que corre el test.

`src/LifeTrack.Domain/Finance/Transaction.cs`

```csharp
using LifeTrack.Domain.Common;

namespace LifeTrack.Domain.Finance;

public sealed class Transaction : Entity
{
    public const int DescriptionMaxLength = 200;
    public const decimal MaxAmount = 1_000_000_000m;

    private Transaction()
    {
    }

    private Transaction(
        Guid userId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string description)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        CategoryId = categoryId;
        Type = type;
        Amount = amount;
        Date = date;
        Description = description;
    }

    public Guid UserId { get; private set; }

    public Guid CategoryId { get; private set; }

    public TransactionType Type { get; private set; }

    public decimal Amount { get; private set; }

    public DateOnly Date { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public static Transaction Create(
        Guid userId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string? description,
        DateOnly today)
    {
        Guard.NotEmpty(userId, "El usuario");
        Guard.NotEmpty(categoryId, "La categoría");

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("El tipo de movimiento no es válido.");
        }

        if (amount <= 0 || amount > MaxAmount)
        {
            throw new DomainException($"El monto debe ser mayor que 0 y no superar {MaxAmount:N0}.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException("El monto admite como máximo 2 decimales.");
        }

        if (date > today)
        {
            throw new DomainException("La fecha del movimiento no puede ser futura.");
        }

        var cleanDescription = description?.Trim() ?? string.Empty;
        if (cleanDescription.Length > DescriptionMaxLength)
        {
            throw new DomainException($"La descripción no puede superar {DescriptionMaxLength} caracteres.");
        }

        return new Transaction(userId, categoryId, type, amount, date, cleanDescription);
    }
}
```

## Budget (fábrica con invariantes)

`src/LifeTrack.Domain/Finance/Budget.cs`

```csharp
using LifeTrack.Domain.Common;

namespace LifeTrack.Domain.Finance;

public sealed class Budget : Entity
{
    private Budget()
    {
    }

    private Budget(Guid userId, Guid categoryId, int year, int month, decimal limit)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        CategoryId = categoryId;
        Year = year;
        Month = month;
        Limit = limit;
    }

    public Guid UserId { get; private set; }

    public Guid CategoryId { get; private set; }

    public int Year { get; private set; }

    public int Month { get; private set; }

    public decimal Limit { get; private set; }

    public static Budget Create(Guid userId, Guid categoryId, int year, int month, decimal limit)
    {
        Guard.NotEmpty(userId, "El usuario");
        Guard.NotEmpty(categoryId, "La categoría");

        if (year is < 2000 or > 2100)
        {
            throw new DomainException("El año del presupuesto no es válido.");
        }

        if (month is < 1 or > 12)
        {
            throw new DomainException("El mes del presupuesto no es válido.");
        }

        if (limit <= 0)
        {
            throw new DomainException("El límite del presupuesto debe ser mayor que 0.");
        }

        if (decimal.Round(limit, 2) != limit)
        {
            throw new DomainException("El límite del presupuesto admite como máximo 2 decimales.");
        }

        return new Budget(userId, categoryId, year, month, limit);
    }
}
```

Los invariantes de `Budget.Create` (año y mes válidos, límite positivo con 2 decimales) son **fallos
silenciosos o de brecha de seguridad**, no ruido: por eso tienen test de dominio, y por eso
el mensaje es en español y seguro de mostrar (ver `AGENTS.md` §10, criterio de riesgo).

