using LifeTrack.Application.Common;
using LifeTrack.Domain.Common;
using MediatR;

namespace LifeTrack.Application.Identity;

public sealed class LoginHandler(
    IAccountService accountService) : IRequestHandler<LoginCommand, LoginResult>
{

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await accountService.LoginAsync(request.Email, request.Password, cancellationToken);
            return new LoginResult(result.Succeeded, result.ErrorMessage);
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
        catch (NotFoundException ex)
        {
            throw new ResourceNotFoundException(ex.Message);
        }
    }
}
