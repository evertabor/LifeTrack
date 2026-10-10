using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Abstractions;

public interface ICategoryRepository
{
    Task AddAsync(Category category, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string normalizedName, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> ListAsync(Guid userId, CancellationToken cancellationToken);
}
