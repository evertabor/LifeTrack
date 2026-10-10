using MediatR;

namespace LifeTrack.Application.Common;

public interface IQuery<TResponse> : IRequest<TResponse>;
