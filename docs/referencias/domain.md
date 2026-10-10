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

## Habit (agregado: check-in, rachas)

`src/LifeTrack.Domain/Habits/Habit.cs`

```csharp
using LifeTrack.Domain.Common;

namespace LifeTrack.Domain.Habits;

public sealed class Habit : Entity
{
    public const int NameMaxLength = 80;

    private readonly List<HabitCheckIn> _checkIns = [];

    private Habit()
    {
    }

    private Habit(Guid userId, string name)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        Name = name;
    }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsArchived { get; private set; }

    public IReadOnlyCollection<HabitCheckIn> CheckIns => _checkIns;

    public static Habit Create(Guid userId, string name) =>
        new(Guard.NotEmpty(userId, "El usuario"), Guard.NotBlank(name, NameMaxLength, "El nombre"));

    public void Archive() => IsArchived = true;

    public void CheckIn(DateOnly date, DateOnly today)
    {
        if (IsArchived)
        {
            throw new DomainException("Un hábito archivado no admite registros.");
        }

        if (date > today)
        {
            throw new DomainException("No se puede registrar un hábito en una fecha futura.");
        }

        if (_checkIns.Any(c => c.Date == date))
        {
            throw new DomainException("El hábito ya está registrado para esa fecha.");
        }

        _checkIns.Add(HabitCheckIn.Create(Id, date));
    }

    public int CurrentStreak(DateOnly today)
    {
        var dates = _checkIns.Select(c => c.Date).ToHashSet();
        var cursor = dates.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;

        while (dates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    public int BestStreak()
    {
        var best = 0;
        var current = 0;
        DateOnly? previous = null;

        foreach (var date in _checkIns.Select(c => c.Date).Distinct().Order())
        {
            current = previous is not null && date == previous.Value.AddDays(1) ? current + 1 : 1;
            best = Math.Max(best, current);
            previous = date;
        }

        return best;
    }
}
```

`CurrentStreak` cuenta días consecutivos **terminados hoy o ayer**: si el usuario aún no ha marcado
hoy, la racha sigue viva porque ayer sí lo hizo. Esa es la diferencia con una cuenta ingenua que
contaría cero en cuanto fallara el check-in de hoy, y por eso el método recibe `today` en lugar de
leer el reloj. `BestStreak` recorre los días distintos y ordenados y guarda la mayor serie, así que
un hueco de un solo día ya la parte en dos.

Los tres invariantes de `CheckIn` (hábito archivado, fecha futura, check-in duplicado) son **fallos
silenciosos o de brecha de seguridad**, no ruido: por eso los tres tienen test de dominio, y por eso
el mensaje es en español y seguro de mostrar (ver `AGENTS.md` §10, criterio de riesgo).

