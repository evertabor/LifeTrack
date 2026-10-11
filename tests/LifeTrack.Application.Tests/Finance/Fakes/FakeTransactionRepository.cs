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

    public Task<Transaction?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_transactions.FirstOrDefault(t => t.Id == id && t.UserId == userId));
    }

    public Task<bool> ExistsForCategoryAsync(Guid categoryId, Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_transactions.Any(t => t.CategoryId == categoryId && t.UserId == userId));
    }

    public void Remove(Transaction transaction)
    {
        _transactions.Remove(transaction);
    }

    public Task<IReadOnlyList<Transaction>> ListByPeriodAsync(
        Guid userId,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken cancellationToken,
        Guid? categoryId = null)
    {
        return Task.FromResult<IReadOnlyList<Transaction>>(
            _transactions
                .Where(t => t.UserId == userId && t.Date >= from && t.Date < toExclusive
                    && (!categoryId.HasValue || t.CategoryId == categoryId.Value))
                .ToList());
    }
}
