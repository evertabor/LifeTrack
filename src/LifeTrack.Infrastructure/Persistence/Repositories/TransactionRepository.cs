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

    public async Task<IReadOnlyList<Transaction>> ListByPeriodAsync(
        Guid userId,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken cancellationToken)
    {
        return await context.Transactions
            .Where(t => t.UserId == userId && t.Date >= from && t.Date < toExclusive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
