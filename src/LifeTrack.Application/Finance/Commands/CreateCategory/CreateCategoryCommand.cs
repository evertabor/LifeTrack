using LifeTrack.Application.Common;

namespace LifeTrack.Application.Finance.Commands.CreateCategory;

public sealed record CreateCategoryCommand(string Name) : ICommand<CategoryResult>;
