using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Abstractions;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> ListByPeriodAsync(
        Guid userId,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken cancellationToken);
}
