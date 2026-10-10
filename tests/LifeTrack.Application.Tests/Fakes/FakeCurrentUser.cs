using LifeTrack.Application.Abstractions;

namespace LifeTrack.Application.Tests.Fakes;

public class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(Guid? userId) => UserId = userId;

    public Guid? UserId { get; }
}
