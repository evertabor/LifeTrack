using LifeTrack.Domain.Common;

namespace LifeTrack.Domain.Finance;

public sealed class Budget : Entity
{
    public const decimal MaxLimit = 1_000_000_000m;
    public const int MinYear = 2000;
    public const int MaxYear = 2100;

    private Budget(
        Guid id,
        Guid userId,
        Guid categoryId,
        int year,
        int month,
        decimal limit)
        : base(id)
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

    public static Budget Create(
        Guid userId,
        Guid categoryId,
        int year,
        int month,
        decimal limit)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("El usuario es obligatorio.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("La categoría es obligatoria.");
        }

        ValidatePeriod(year, month);
        ValidateLimit(limit);

        return new Budget(Guid.NewGuid(), userId, categoryId, year, month, limit);
    }

    public void ChangeLimit(decimal limit)
    {
        ValidateLimit(limit);
        Limit = limit;
    }

    private static void ValidatePeriod(int year, int month)
    {
        if (year < MinYear || year > MaxYear || month < 1 || month > 12)
        {
            throw new DomainException("El periodo del presupuesto no es válido.");
        }
    }

    private static void ValidateLimit(decimal limit)
    {
        if (limit <= 0 || limit > MaxLimit)
        {
            throw new DomainException("El límite debe ser mayor que 0 y no superar 1.000.000.000.");
        }

        if (decimal.Round(limit, 2) != limit)
        {
            throw new DomainException("El límite admite como máximo 2 decimales.");
        }
    }
}
