using MediatR;

namespace LifeTrack.Application.Finance.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest;
