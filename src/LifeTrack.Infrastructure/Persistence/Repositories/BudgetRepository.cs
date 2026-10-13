using LifeTrack.Application.Abstractions;
using LifeTrack.Domain.Finance;
using LifeTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.Infrastructure.Persistence.Repositories;

public sealed class BudgetRepository(
    AppDbContext context) : IBudgetRepository
{
    public async Task AddAsync(Budget budget, CancellationToken cancellationToken)
    {
        await context.Budgets.AddAsync(budget, cancellationToken);
    }

    public async Task<Budget?> GetAsync(Guid categoryId, Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        return await context.Budgets
            .FirstOrDefaultAsync(b => b.CategoryId == categoryId && b.UserId == userId && b.Year == year && b.Month == month, cancellationToken);
    }

    public async Task<bool> ExistsForCategoryAsync(Guid categoryId, Guid userId, CancellationToken cancellationToken)
    {
        return await context.Budgets
            .AnyAsync(b => b.CategoryId == categoryId && b.UserId == userId, cancellationToken);
    }

    public void Remove(Budget budget)
    {
        context.Budgets.Remove(budget);
    }

    public async Task<IReadOnlyList<Budget>> ListByPeriodAsync(Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        return await context.Budgets
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
