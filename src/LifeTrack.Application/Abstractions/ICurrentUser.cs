namespace LifeTrack.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
