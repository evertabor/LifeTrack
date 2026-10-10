using MediatR;

namespace LifeTrack.Application.Common;

public interface ICommand<TResponse> : IRequest<TResponse>;
