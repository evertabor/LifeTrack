using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Abstractions;

public interface IBudgetRepository
{
    Task AddAsync(Budget budget, CancellationToken cancellationToken);

    Task<Budget?> GetAsync(Guid categoryId, Guid userId, int year, int month, CancellationToken cancellationToken);

    Task<bool> ExistsForCategoryAsync(Guid categoryId, Guid userId, CancellationToken cancellationToken);

    void Remove(Budget budget);

    Task<IReadOnlyList<Budget>> ListByPeriodAsync(Guid userId, int year, int month, CancellationToken cancellationToken);
}
