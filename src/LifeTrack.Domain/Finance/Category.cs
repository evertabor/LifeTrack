using LifeTrack.Domain.Common;

namespace LifeTrack.Domain.Finance;

public sealed class Category : Entity
{
    public const int NameMaxLength = 50;

    public Guid UserId { get; private set; }
    public string Name { get; private set; }

    public TransactionType Type { get; private set; }

    private Category()
        : base(Guid.Empty)
    {
        Name = string.Empty;
    }

    private Category(Guid id, Guid userId, string name, TransactionType type) : base(id)
    {
        UserId = userId;
        Name = name;
        Type = type;
    }

    public static Category Create(string? name, Guid userId, TransactionType type)
    {
        var normalizedName = NormalizeName(name);
        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new DomainException("El nombre de la categoría es obligatorio.");
        }
        if (normalizedName.Length > NameMaxLength)
        {
            throw new DomainException($"El nombre de la categoría no puede superar los {NameMaxLength} caracteres.");
        }
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("El tipo de la categoría no es válido.");
        }
        return new Category(Guid.NewGuid(), userId, normalizedName, type);
    }

    public void EditName(string? name)
    {
        var normalizedName = NormalizeName(name);
        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new DomainException("El nombre de la categoría es obligatorio.");
        }
        if (normalizedName.Length > NameMaxLength)
        {
            throw new DomainException($"El nombre de la categoría no puede superar los {NameMaxLength} caracteres.");
        }
        Name = normalizedName;
    }

    private static string NormalizeName(string? name) => name?.Trim() ?? string.Empty;
}
