using LifeTrack.Application.Abstractions;
using LifeTrack.Domain.Finance;

namespace LifeTrack.Application.Tests.Finance.Fakes;

public sealed class FakeCategoryRepository : ICategoryRepository
{
    private readonly List<Category> _categories = [];

    public Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        _categories.Add(category);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string normalizedName, Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_categories.Any(c =>
            c.UserId == userId && c.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<Category?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_categories.FirstOrDefault(c => c.Id == id && c.UserId == userId));
    }

    public Task RemoveAsync(Category category, CancellationToken cancellationToken)
    {
        _categories.Remove(category);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Category>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Category>>(
            _categories.Where(c => c.UserId == userId).ToList());
    }

    public void Reset() => _categories.Clear();
}
