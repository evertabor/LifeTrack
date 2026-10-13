using LifeTrack.Application.Abstractions;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance.Fakes;

public sealed class FakeBudgetRepository : IBudgetRepository
{
    private readonly List<Budget> _budgets = [];

    public Task AddAsync(Budget budget, CancellationToken cancellationToken)
    {
        _budgets.Add(budget);
        return Task.CompletedTask;
    }

    public Task<Budget?> GetAsync(Guid categoryId, Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        return Task.FromResult(_budgets.FirstOrDefault(b =>
            b.CategoryId == categoryId && b.UserId == userId && b.Year == year && b.Month == month));
    }

    public Task<bool> ExistsForCategoryAsync(Guid categoryId, Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_budgets.Any(b => b.CategoryId == categoryId && b.UserId == userId));
    }

    public Task<IReadOnlyList<Budget>> ListByPeriodAsync(Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Budget>>(
            _budgets.Where(b => b.UserId == userId && b.Year == year && b.Month == month).ToList());
    }

    public void Remove(Budget budget)
    {
        _budgets.Remove(budget);
    }
}
