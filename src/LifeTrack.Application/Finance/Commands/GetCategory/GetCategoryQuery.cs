using LifeTrack.Application.Common;
using LifeTrack.Application.Finance.Commands.ListCategories;

namespace LifeTrack.Application.Finance.Commands.GetCategory;

public sealed record GetCategoryQuery(Guid Id) : IQuery<CategorySummary>;
