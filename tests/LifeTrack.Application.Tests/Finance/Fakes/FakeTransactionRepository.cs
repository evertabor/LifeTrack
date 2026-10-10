using LifeTrack.Application.Abstractions;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance.Fakes;

public sealed class FakeTransactionRepository : ITransactionRepository
{
    private readonly List<Transaction> _transactions = [];

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        _transactions.Add(transaction);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Transaction>> ListByPeriodAsync(
        Guid userId,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Transaction>>(
            _transactions
                .Where(t => t.UserId == userId && t.Date >= from && t.Date < toExclusive)
                .ToList());
    }
}
