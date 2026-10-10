using LifeTrack.Domain.Common;

namespace LifeTrack.Domain.Finance;

public sealed class Transaction : Entity
{
    public const int DescriptionMaxLength = 200;
    public const decimal MaxAmount = 1_000_000_000m;

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

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("El tipo de movimiento no es válido.");
        }

        if (amount <= 0 || amount > MaxAmount)
        {
            throw new DomainException("El monto debe ser mayor que 0 y no superar 1.000.000.000.");
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
            throw new DomainException("La descripción no puede superar 200 caracteres.");
        }

        return new Transaction(Guid.NewGuid(), userId, categoryId, type, amount, date, cleanDescription);
    }
}
