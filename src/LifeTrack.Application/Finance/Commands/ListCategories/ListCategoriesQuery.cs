using LifeTrack.Application.Common;

namespace LifeTrack.Application.Finance.Commands.ListCategories;

public sealed record ListCategoriesQuery : IQuery<IReadOnlyList<CategorySummary>>;
