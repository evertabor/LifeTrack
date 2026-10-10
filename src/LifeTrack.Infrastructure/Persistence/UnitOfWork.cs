using LifeTrack.Application.Abstractions;

namespace LifeTrack.Infrastructure.Persistence;

public sealed class UnitOfWork(
    AppDbContext context) : IUnitOfWork
{

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
