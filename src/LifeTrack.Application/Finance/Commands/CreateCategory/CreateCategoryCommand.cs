using LifeTrack.Application.Common;

namespace LifeTrack.Application.Finance.Commands.CreateCategory;

public sealed record CreateCategoryCommand(string Name, bool IsIncome) : ICommand<CategoryResult>;
