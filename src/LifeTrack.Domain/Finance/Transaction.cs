using LifeTrack.Domain.Common;

namespace LifeTrack.Domain.Finance;

public sealed class Transaction : Entity
{
    public const int DescriptionMaxLength = 200;
    public const decimal MaxAmount = 1_000_000_000m;

    private const int DecimalPlaces = 2;

    private Transaction(
        Guid id,
        Guid userId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string description)
        : base(id)
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

    public string Description { get; private set; }

    public static Transaction Create(
        Guid userId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string? description,
        DateOnly today)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("El usuario es obligatorio.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("La categoría es obligatoria.");
        }

        var cleanDescription = Validate(type, amount, date, description, today);

        return new Transaction(Guid.NewGuid(), userId, categoryId, type, amount, date, cleanDescription);
    }

    public void Update(
        Guid categoryId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string? description,
        DateOnly today)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("La categoría es obligatoria.");
        }

        var cleanDescription = Validate(type, amount, date, description, today);

        CategoryId = categoryId;
        Type = type;
        Amount = amount;
        Date = date;
        Description = cleanDescription;
    }

    private static string Validate(
        TransactionType type,
        decimal amount,
        DateOnly date,
        string? description,
        DateOnly today)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("El tipo de movimiento no es válido.");
        }

        if (amount <= 0 || amount > MaxAmount)
        {
            throw new DomainException("El monto debe ser mayor que 0 y no superar 1.000.000.000.");
        }

        if (decimal.Round(amount, DecimalPlaces) != amount)
        {
            throw new DomainException($"El monto admite como máximo {DecimalPlaces} decimales.");
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

        return cleanDescription;
    }
}
