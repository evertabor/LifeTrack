using MediatR;

namespace LifeTrack.Application.Finance.Commands.EditCategory;

public sealed record EditCategoryCommand(Guid Id, string Name) : IRequest;
