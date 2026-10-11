using LifeTrack.Application.Abstractions;
using LifeTrack.Domain.Finance;
using LifeTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository(
    AppDbContext context) : ITransactionRepository
{
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        await context.Transactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
    }

    public Task RemoveAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        context.Transactions.Remove(transaction);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Transaction>> ListByPeriodAsync(
        Guid userId,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken cancellationToken,
        Guid? categoryId = null)
    {
        var query = context.Transactions
            .Where(t => t.UserId == userId && t.Date >= from && t.Date < toExclusive);

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
