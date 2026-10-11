using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Abstractions;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken);

    Task<Transaction?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<bool> ExistsForCategoryAsync(Guid categoryId, Guid userId, CancellationToken cancellationToken);

    Task RemoveAsync(Transaction transaction, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> ListByPeriodAsync(
        Guid userId,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken cancellationToken,
        Guid? categoryId = null);
}
