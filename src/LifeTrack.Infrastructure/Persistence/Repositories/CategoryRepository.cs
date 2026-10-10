using LifeTrack.Application.Abstractions;
using LifeTrack.Domain.Finance;
using LifeTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(
    AppDbContext context) : ICategoryRepository
{

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        await context.Categories.AddAsync(category, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string normalizedName, Guid userId, CancellationToken cancellationToken)
    {
        return await context.Categories
            .AnyAsync(c => c.UserId == userId && c.Name == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.Categories
            .Where(c => c.UserId == userId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
