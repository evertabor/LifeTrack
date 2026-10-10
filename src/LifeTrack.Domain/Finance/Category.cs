using LifeTrack.Domain.Common;

namespace LifeTrack.Domain.Finance;

public sealed class Category : Entity
{
    public const int NameMaxLength = 50;

    public Guid UserId { get; }
    public string Name { get; }

    private Category(Guid id, Guid userId, string name) : base(id)
    {
        UserId = userId;
        Name = name;
    }

    public static Category Create(string name, Guid userId)
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
        return new Category(Guid.NewGuid(), userId, normalizedName);
    }

    private static string NormalizeName(string name) => name.Trim();
}
