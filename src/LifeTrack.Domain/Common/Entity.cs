namespace LifeTrack.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; }

    protected Entity(Guid id) => Id = id;

    public override bool Equals(object? other) =>
        other is Entity entity && Id.Equals(entity.Id);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) =>
        Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) =>
        !Equals(left, right);
}
